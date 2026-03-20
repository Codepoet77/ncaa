using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Data;
using NcaaBracket.Api.DTOs;
using NcaaBracket.Api.Models;

namespace NcaaBracket.Api.Controllers;

[ApiController]
[Route("api/picks")]
[Authorize]
public class PicksController : ControllerBase
{
    private readonly AppDbContext _db;

    public PicksController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }

    [HttpGet]
    public async Task<ActionResult<List<UserPickDto>>> GetPicks()
    {
        var userId = GetUserId();

        var picks = await _db.UserPicks
            .Where(p => p.UserId == userId)
            .Include(p => p.PickedTeam)
            .OrderBy(p => p.GameId)
            .Select(p => new UserPickDto
            {
                Id = p.Id,
                GameId = p.GameId,
                PickedTeamId = p.PickedTeamId,
                PickedTeamName = p.PickedTeam.Name,
                IsCorrect = p.IsCorrect,
                PointsEarned = p.PointsEarned
            })
            .ToListAsync();

        return Ok(picks);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitPicks([FromBody] SubmitPicksRequest request)
    {
        var settings = await _db.TournamentSettings.FirstOrDefaultAsync();
        if (settings is not null && settings.IsLocked)
            return BadRequest(new { message = "Tournament is locked. Picks can no longer be submitted." });

        var userId = GetUserId();

        // Validate all game IDs and team IDs exist
        var gameIds = request.Picks.Select(p => p.GameId).ToList();
        var games = await _db.Games.Where(g => gameIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id);

        var teamIds = request.Picks.Select(p => p.PickedTeamId).ToHashSet();
        var validTeamIds = await _db.Teams.Where(t => teamIds.Contains(t.Id)).Select(t => t.Id).ToHashSetAsync();

        // Build set of eliminated teams
        var completedGames = await _db.Games
            .Where(g => g.IsCompleted && g.WinnerId != null)
            .Select(g => new { g.Team1Id, g.Team2Id, g.WinnerId })
            .ToListAsync();
        var eliminatedTeamIds = new HashSet<int>();
        foreach (var cg in completedGames)
        {
            if (cg.Team1Id.HasValue && cg.Team1Id != cg.WinnerId)
                eliminatedTeamIds.Add(cg.Team1Id.Value);
            if (cg.Team2Id.HasValue && cg.Team2Id != cg.WinnerId)
                eliminatedTeamIds.Add(cg.Team2Id.Value);
        }

        foreach (var pick in request.Picks)
        {
            if (!games.ContainsKey(pick.GameId))
                return BadRequest(new { message = $"Game {pick.GameId} not found" });
            if (!validTeamIds.Contains(pick.PickedTeamId))
                return BadRequest(new { message = $"Team {pick.PickedTeamId} not found" });
        }

        // Get existing picks for this user
        var existingPicks = await _db.UserPicks
            .Where(p => p.UserId == userId && gameIds.Contains(p.GameId))
            .ToDictionaryAsync(p => p.GameId);

        foreach (var pick in request.Picks)
        {
            var game = games[pick.GameId];

            // Don't allow changing picks for completed games
            if (game.IsCompleted)
                continue;

            // Don't allow picking eliminated teams
            if (eliminatedTeamIds.Contains(pick.PickedTeamId))
                continue;

            if (existingPicks.TryGetValue(pick.GameId, out var existing))
            {
                existing.PickedTeamId = pick.PickedTeamId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.UserPicks.Add(new UserPick
                {
                    UserId = userId,
                    GameId = pick.GameId,
                    PickedTeamId = pick.PickedTeamId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Picks saved successfully" });
    }

    [HttpGet("user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserPicks(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        var picks = await _db.UserPicks
            .Where(p => p.UserId == userId)
            .Include(p => p.PickedTeam)
            .Include(p => p.Game)
            .OrderBy(p => p.GameId)
            .ToListAsync();

        var stats = CalculatePickStats(picks);

        return Ok(new
        {
            user = new { user.Id, user.DisplayName, user.BracketTitle },
            picks = picks.Select(p => new UserPickDto
            {
                Id = p.Id,
                GameId = p.GameId,
                PickedTeamId = p.PickedTeamId,
                PickedTeamName = p.PickedTeam.Name,
                IsCorrect = p.IsCorrect,
                PointsEarned = p.PointsEarned
            }),
            stats
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetPickStats()
    {
        var userId = GetUserId();
        var picks = await _db.UserPicks
            .Where(p => p.UserId == userId)
            .Include(p => p.Game)
            .ToListAsync();

        return Ok(CalculatePickStats(picks));
    }

    private object CalculatePickStats(List<UserPick> picks)
    {
        // Get eliminated teams
        var completedGames = _db.Games
            .Where(g => g.IsCompleted && g.WinnerId != null)
            .Select(g => new { g.Team1Id, g.Team2Id, g.WinnerId })
            .ToList();

        var eliminatedTeamIds = new HashSet<int>();
        foreach (var game in completedGames)
        {
            if (game.Team1Id.HasValue && game.Team1Id != game.WinnerId)
                eliminatedTeamIds.Add(game.Team1Id.Value);
            if (game.Team2Id.HasValue && game.Team2Id != game.WinnerId)
                eliminatedTeamIds.Add(game.Team2Id.Value);
        }

        var totalPoints = picks.Sum(p => p.PointsEarned);
        var correctPicks = picks.Count(p => p.IsCorrect == true);
        var pendingPoints = picks
            .Where(p => p.IsCorrect == null && !eliminatedTeamIds.Contains(p.PickedTeamId))
            .Sum(p => p.Game?.Round ?? 0);

        return new
        {
            totalPoints,
            maxPossiblePoints = totalPoints + pendingPoints,
            correctPicks
        };
    }

    [HttpGet("title")]
    public async Task<IActionResult> GetBracketTitle()
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();
        return Ok(new { title = user.BracketTitle ?? $"{user.DisplayName}'s Bracket" });
    }

    [HttpPut("title")]
    public async Task<IActionResult> UpdateBracketTitle([FromBody] UpdateBracketTitleRequest request)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.BracketTitle = request.Title;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Bracket title updated", title = user.BracketTitle });
    }
}
