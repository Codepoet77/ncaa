using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Data;
using NcaaBracket.Api.DTOs;

namespace NcaaBracket.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaderboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaderboardEntry>>> GetLeaderboard()
    {
        // Get all eliminated teams (teams that lost a completed game)
        var completedGames = await _db.Games
            .Where(g => g.IsCompleted && g.WinnerId != null)
            .Select(g => new { g.Team1Id, g.Team2Id, g.WinnerId })
            .ToListAsync();

        var eliminatedTeamIds = new HashSet<int>();
        foreach (var game in completedGames)
        {
            if (game.Team1Id.HasValue && game.Team1Id != game.WinnerId)
                eliminatedTeamIds.Add(game.Team1Id.Value);
            if (game.Team2Id.HasValue && game.Team2Id != game.WinnerId)
                eliminatedTeamIds.Add(game.Team2Id.Value);
        }

        // Get all users with their picks and associated game rounds
        var users = await _db.Users
            .Include(u => u.UserPicks)
            .ThenInclude(p => p.Game)
            .ToListAsync();

        var entries = users.Select(u =>
        {
            var earnedPoints = u.UserPicks.Sum(p => p.PointsEarned);
            var correctPicks = u.UserPicks.Count(p => p.IsCorrect == true);

            // Max possible = earned points + points from pending picks where team is still alive
            var pendingPoints = u.UserPicks
                .Where(p => p.IsCorrect == null && !eliminatedTeamIds.Contains(p.PickedTeamId))
                .Sum(p => p.Game?.Round ?? 0);

            return new LeaderboardEntry
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                BracketTitle = u.BracketTitle ?? u.DisplayName + "'s Bracket",
                TotalPoints = earnedPoints,
                MaxPossiblePoints = earnedPoints + pendingPoints,
                CorrectPicks = correctPicks
            };
        })
        .OrderByDescending(e => e.TotalPoints)
        .ThenByDescending(e => e.MaxPossiblePoints)
        .ThenByDescending(e => e.CorrectPicks)
        .ToList();

        for (var i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }

        return Ok(entries);
    }
}
