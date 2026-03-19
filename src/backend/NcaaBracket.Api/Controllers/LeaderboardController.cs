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
        var entries = await _db.Users
            .Select(u => new LeaderboardEntry
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                BracketTitle = u.BracketTitle ?? u.DisplayName + "'s Bracket",
                TotalPoints = u.UserPicks.Sum(p => p.PointsEarned),
                CorrectPicks = u.UserPicks.Count(p => p.IsCorrect == true)
            })
            .OrderByDescending(e => e.TotalPoints)
            .ThenByDescending(e => e.CorrectPicks)
            .ToListAsync();

        // Assign ranks
        for (var i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }

        return Ok(entries);
    }
}
