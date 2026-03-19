using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Data;

namespace NcaaBracket.Api.Services;

public class ScoringService
{
    private readonly AppDbContext _db;

    public ScoringService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Scores all picks for a completed game. Points by round: round N = N points.
    /// </summary>
    public async Task ScoreGameAsync(int gameId)
    {
        var game = await _db.Games.FindAsync(gameId);
        if (game is null || !game.IsCompleted || game.WinnerId is null)
            return;

        var picks = await _db.UserPicks
            .Where(p => p.GameId == gameId)
            .ToListAsync();

        foreach (var pick in picks)
        {
            pick.IsCorrect = pick.PickedTeamId == game.WinnerId;
            pick.PointsEarned = pick.IsCorrect == true ? game.Round : 0;
            pick.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}
