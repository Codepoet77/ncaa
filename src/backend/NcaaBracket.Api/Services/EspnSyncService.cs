using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Data;
using NcaaBracket.Api.Models;

namespace NcaaBracket.Api.Services;

public class EspnSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EspnSyncService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);

    // Standard NCAA bracket position by seed matchup (1v16=pos1, 8v9=pos2, etc.)
    private static readonly Dictionary<int, int> SeedToBracketPosition = new()
    {
        { 1, 1 }, { 16, 1 },
        { 8, 2 }, { 9, 2 },
        { 5, 3 }, { 12, 3 },
        { 4, 4 }, { 13, 4 },
        { 6, 5 }, { 11, 5 },
        { 3, 6 }, { 14, 6 },
        { 7, 7 }, { 10, 7 },
        { 2, 8 }, { 15, 8 },
    };

    public EspnSyncService(IServiceScopeFactory scopeFactory, ILogger<EspnSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ESPN sync");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Auto-lock tournament if lock_date has passed
        var settings = await db.TournamentSettings.FirstOrDefaultAsync(ct);
        if (settings is not null && !settings.IsLocked && DateTime.UtcNow >= settings.LockDate)
        {
            settings.IsLocked = true;
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Tournament locked");
        }

        var today = DateTime.UtcNow.Date;
        var startDate = new DateTime(today.Year, 3, 18);
        var endDate = new DateTime(today.Year, 4, 8);

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            var dateStr = d.ToString("yyyyMMdd");
            var url = $"https://site.api.espn.com/apis/site/v2/sports/basketball/mens-college-basketball/scoreboard?groups=100&limit=100&dates={dateStr}";

            _logger.LogInformation("Syncing ESPN data for {Date}", dateStr);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode) continue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch ESPN data for {Date}", dateStr);
                continue;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("events", out var events)) continue;

            foreach (var evt in events.EnumerateArray())
            {
                await ProcessEventAsync(db, evt, ct);
            }
        }

        if (settings is not null)
        {
            settings.LastEspnSync = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessEventAsync(AppDbContext db, JsonElement evt, CancellationToken ct)
    {
        var espnGameId = evt.GetProperty("id").GetString();
        if (espnGameId is null) return;

        if (!evt.TryGetProperty("competitions", out var competitions)) return;
        var competition = competitions[0];

        if (!competition.TryGetProperty("competitors", out var competitors)) return;
        if (competitors.GetArrayLength() < 2) return;

        var comp1 = competitors[0];
        var comp2 = competitors[1];

        var team1EspnId = comp1.GetProperty("id").GetString();
        var team2EspnId = comp2.GetProperty("id").GetString();

        var team1Name = comp1.GetProperty("team").GetProperty("displayName").GetString() ?? "TBD";
        var team2Name = comp2.GetProperty("team").GetProperty("displayName").GetString() ?? "TBD";

        var team1ShortName = comp1.GetProperty("team").TryGetProperty("shortDisplayName", out var sn1)
            ? sn1.GetString() : null;
        var team2ShortName = comp2.GetProperty("team").TryGetProperty("shortDisplayName", out var sn2)
            ? sn2.GetString() : null;

        var team1Logo = comp1.GetProperty("team").TryGetProperty("logo", out var logo1)
            ? logo1.GetString() : null;
        var team2Logo = comp2.GetProperty("team").TryGetProperty("logo", out var logo2)
            ? logo2.GetString() : null;

        int.TryParse(comp1.TryGetProperty("curatedRank", out var rank1)
            ? rank1.TryGetProperty("current", out var r1) ? r1.GetRawText() : "0" : "0", out var team1Seed);
        int.TryParse(comp2.TryGetProperty("curatedRank", out var rank2)
            ? rank2.TryGetProperty("current", out var r2) ? r2.GetRawText() : "0" : "0", out var team2Seed);

        if (comp1.TryGetProperty("seed", out var seed1Prop))
            int.TryParse(seed1Prop.GetString() ?? seed1Prop.GetRawText(), out team1Seed);
        if (comp2.TryGetProperty("seed", out var seed2Prop))
            int.TryParse(seed2Prop.GetString() ?? seed2Prop.GetRawText(), out team2Seed);

        // Determine region and round from competition notes
        var region = "TBD";
        var round = 1;
        var isFirstFour = false;

        if (competition.TryGetProperty("notes", out var notes) && notes.GetArrayLength() > 0)
        {
            var headline = notes[0].TryGetProperty("headline", out var h) ? h.GetString() ?? "" : "";
            region = ParseRegion(headline);
            round = ParseRound(headline);
            isFirstFour = headline.Contains("First Four", StringComparison.OrdinalIgnoreCase);
        }

        // Determine bracket position
        int bracketPosition;
        if (isFirstFour)
        {
            round = 0;
            var existingFF = await db.Games.CountAsync(
                g => g.Round == 0 && g.Region == region && g.EspnId != espnGameId, ct);
            bracketPosition = existingFF + 1;
        }
        else if (round == 1)
        {
            // Round 1: use seed to determine correct bracket position
            var higherSeed = Math.Min(team1Seed, team2Seed);
            bracketPosition = SeedToBracketPosition.GetValueOrDefault(higherSeed, 0);
            if (bracketPosition == 0)
            {
                var maxPos = await db.Games.Where(g => g.Round == 1 && g.Region == region)
                    .MaxAsync(g => (int?)g.BracketPosition, ct) ?? 0;
                bracketPosition = maxPos + 1;
            }
        }
        else
        {
            // R2+: determine position by matching teams to feeder games from the previous round
            bracketPosition = await CalculateLaterRoundPosition(db, round, region, team1EspnId, team2EspnId, ct);

            if (bracketPosition == 0)
            {
                // Can't determine from teams (both TBD) — find next available position
                var takenPositions = await db.Games
                    .Where(g => g.Round == round && g.Region == region && g.EspnId != espnGameId)
                    .Select(g => g.BracketPosition)
                    .ToListAsync(ct);

                var expectedCount = round switch
                {
                    2 => 4, 3 => 2, 4 => 1, 5 => 2, 6 => 1, _ => 4
                };

                for (var pos = 1; pos <= expectedCount; pos++)
                {
                    if (!takenPositions.Contains(pos))
                    {
                        bracketPosition = pos;
                        break;
                    }
                }

                if (bracketPosition == 0)
                    bracketPosition = takenPositions.Count + 1;
            }
        }

        // Scores
        int.TryParse(comp1.TryGetProperty("score", out var s1) ? s1.GetString() ?? "0" : "0", out var team1Score);
        int.TryParse(comp2.TryGetProperty("score", out var s2) ? s2.GetString() ?? "0" : "0", out var team2Score);

        // Status
        var isCompleted = false;
        string? winnerEspnId = null;

        if (competition.TryGetProperty("status", out var status) &&
            status.TryGetProperty("type", out var statusType))
        {
            isCompleted = statusType.TryGetProperty("completed", out var c) && c.GetBoolean();
        }

        if (isCompleted)
        {
            var winner1 = comp1.TryGetProperty("winner", out var w1) && w1.GetBoolean();
            winnerEspnId = winner1 ? team1EspnId : team2EspnId;
        }

        // Game time
        DateTime? gameTime = null;
        if (competition.TryGetProperty("date", out var dateEl))
        {
            if (DateTime.TryParse(dateEl.GetString(), out var parsed))
                gameTime = parsed.ToUniversalTime();
        }

        // Upsert teams (skip TBD placeholder teams)
        Team? team1 = null;
        Team? team2 = null;

        if (team1Name != "TBD")
        {
            team1 = await db.Teams.FirstOrDefaultAsync(t => t.EspnId == team1EspnId, ct);
            if (team1 is null)
            {
                team1 = new Team
                {
                    EspnId = team1EspnId, Name = team1Name, Seed = team1Seed,
                    Region = region, LogoUrl = team1Logo, ShortName = team1ShortName
                };
                db.Teams.Add(team1);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                team1.Name = team1Name;
                team1.Seed = team1Seed;
                if (region != "TBD") team1.Region = region;
                team1.LogoUrl = team1Logo;
                team1.ShortName = team1ShortName;
            }
        }

        if (team2Name != "TBD")
        {
            team2 = await db.Teams.FirstOrDefaultAsync(t => t.EspnId == team2EspnId, ct);
            if (team2 is null)
            {
                team2 = new Team
                {
                    EspnId = team2EspnId, Name = team2Name, Seed = team2Seed,
                    Region = region, LogoUrl = team2Logo, ShortName = team2ShortName
                };
                db.Teams.Add(team2);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                team2.Name = team2Name;
                team2.Seed = team2Seed;
                if (region != "TBD") team2.Region = region;
                team2.LogoUrl = team2Logo;
                team2.ShortName = team2ShortName;
            }
        }

        // Upsert game
        var game = await db.Games.FirstOrDefaultAsync(g => g.EspnId == espnGameId, ct);
        var wasCompleted = game?.IsCompleted ?? false;

        if (game is null)
        {
            game = new Game
            {
                EspnId = espnGameId,
                Round = round,
                Region = region,
                BracketPosition = bracketPosition,
                Team1Id = team1?.Id,
                Team2Id = team2?.Id,
                Team1Score = team1Score,
                Team2Score = team2Score,
                GameTime = gameTime,
                IsCompleted = isCompleted
            };
            db.Games.Add(game);
        }
        else
        {
            if (team1 is not null) game.Team1Id = team1.Id;
            if (team2 is not null) game.Team2Id = team2.Id;
            game.Team1Score = team1Score;
            game.Team2Score = team2Score;
            game.IsCompleted = isCompleted;
            game.Round = round;
            game.Region = region;
            game.BracketPosition = bracketPosition;
            if (gameTime.HasValue)
                game.GameTime = gameTime;
        }

        if (isCompleted && winnerEspnId is not null)
        {
            var winner = winnerEspnId == team1EspnId ? team1 : team2;
            if (winner is not null)
                game.WinnerId = winner.Id;
        }

        await db.SaveChangesAsync(ct);

        // Score picks if game just completed
        if (isCompleted && !wasCompleted)
        {
            var scoringService = new ScoringService(db);
            await scoringService.ScoreGameAsync(game.Id);
        }
    }

    /// <summary>
    /// For R2+ games, determine bracket position by finding which previous-round games
    /// contain the same teams. R2 pos = ceil(feederR1pos / 2), etc.
    /// </summary>
    private async Task<int> CalculateLaterRoundPosition(
        AppDbContext db, int round, string region,
        string? team1EspnId, string? team2EspnId, CancellationToken ct)
    {
        var prevRound = round - 1;
        var prevRegion = region;

        // Final Four games come from Elite 8 in specific regions
        if (round == 5 && region == "Final Four")
        {
            // Can't determine position from teams alone for Final Four
            // Use team's region to figure out which FF game
            if (team1EspnId != null || team2EspnId != null)
            {
                var espnId = team1EspnId ?? team2EspnId;
                var team = await db.Teams.FirstOrDefaultAsync(t => t.EspnId == espnId, ct);
                if (team != null)
                {
                    return (team.Region == "East" || team.Region == "West") ? 1 : 2;
                }
            }
            return 0;
        }

        // Championship
        if (round == 6)
        {
            return 1;
        }

        // For R2-R4: find feeder games from previous round in the same region
        var prevGames = await db.Games
            .Where(g => g.Round == prevRound && g.Region == prevRegion)
            .Include(g => g.Team1)
            .Include(g => g.Team2)
            .ToListAsync(ct);

        if (prevGames.Count == 0) return 0;

        // Find which previous-round game contains one of our teams
        foreach (var prevGame in prevGames)
        {
            var prevTeamEspnIds = new HashSet<string?> {
                prevGame.Team1?.EspnId, prevGame.Team2?.EspnId
            };

            if ((team1EspnId != null && prevTeamEspnIds.Contains(team1EspnId)) ||
                (team2EspnId != null && prevTeamEspnIds.Contains(team2EspnId)))
            {
                // This previous game feeds into our game
                // Position = ceil(prevPosition / 2)
                return (int)Math.Ceiling(prevGame.BracketPosition / 2.0);
            }

            // Also check winners
            if (prevGame.WinnerId != null)
            {
                var winner = prevGame.Team1?.Id == prevGame.WinnerId ? prevGame.Team1 : prevGame.Team2;
                if (winner != null &&
                    ((team1EspnId != null && winner.EspnId == team1EspnId) ||
                     (team2EspnId != null && winner.EspnId == team2EspnId)))
                {
                    return (int)Math.Ceiling(prevGame.BracketPosition / 2.0);
                }
            }
        }

        return 0;
    }

    private static string ParseRegion(string headline)
    {
        var regions = new[] { "South", "East", "Midwest", "West", "Final Four" };
        foreach (var r in regions)
        {
            if (headline.Contains(r, StringComparison.OrdinalIgnoreCase))
                return r;
        }
        return "TBD";
    }

    private static int ParseRound(string headline)
    {
        if (headline.Contains("First Four", StringComparison.OrdinalIgnoreCase)) return 0;
        if (headline.Contains("1st Round", StringComparison.OrdinalIgnoreCase) ||
            headline.Contains("First Round", StringComparison.OrdinalIgnoreCase)) return 1;
        if (headline.Contains("2nd Round", StringComparison.OrdinalIgnoreCase) ||
            headline.Contains("Second Round", StringComparison.OrdinalIgnoreCase)) return 2;
        if (headline.Contains("Sweet 16", StringComparison.OrdinalIgnoreCase) ||
            headline.Contains("Sweet Sixteen", StringComparison.OrdinalIgnoreCase)) return 3;
        if (headline.Contains("Elite Eight", StringComparison.OrdinalIgnoreCase) ||
            headline.Contains("Elite 8", StringComparison.OrdinalIgnoreCase)) return 4;
        if (headline.Contains("Final Four", StringComparison.OrdinalIgnoreCase) ||
            headline.Contains("Semifinal", StringComparison.OrdinalIgnoreCase)) return 5;
        if (headline.Contains("National Championship", StringComparison.OrdinalIgnoreCase) ||
            headline.Contains("Championship Game", StringComparison.OrdinalIgnoreCase)) return 6;
        return 1;
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}
