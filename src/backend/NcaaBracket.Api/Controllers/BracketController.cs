using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NcaaBracket.Api.Data;
using NcaaBracket.Api.DTOs;

namespace NcaaBracket.Api.Controllers;

[ApiController]
[Route("api/bracket")]
public class BracketController : ControllerBase
{
    private readonly AppDbContext _db;

    public BracketController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<GameDto>>> GetBracket()
    {
        var games = await _db.Games
            .Include(g => g.Team1)
            .Include(g => g.Team2)
            .OrderBy(g => g.Round)
            .ThenBy(g => g.BracketPosition)
            .ToListAsync();

        return Ok(games.Select(MapGameDto).ToList());
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _db.TournamentSettings.FirstOrDefaultAsync();
        if (settings is null)
            return NotFound();

        return Ok(new
        {
            settings.Year,
            settings.LockDate,
            settings.IsLocked
        });
    }

    private static GameDto MapGameDto(Models.Game game) => new()
    {
        Id = game.Id,
        Round = game.Round,
        Region = game.Region,
        BracketPosition = game.BracketPosition,
        NextGameId = game.NextGameId,
        Slot = game.Slot,
        Team1 = game.Team1 is not null ? new TeamDto
        {
            Id = game.Team1.Id,
            Name = game.Team1.Name,
            ShortName = game.Team1.ShortName,
            Seed = game.Team1.Seed,
            Region = game.Team1.Region,
            LogoUrl = game.Team1.LogoUrl
        } : null,
        Team2 = game.Team2 is not null ? new TeamDto
        {
            Id = game.Team2.Id,
            Name = game.Team2.Name,
            ShortName = game.Team2.ShortName,
            Seed = game.Team2.Seed,
            Region = game.Team2.Region,
            LogoUrl = game.Team2.LogoUrl
        } : null,
        Team1Score = game.Team1Score,
        Team2Score = game.Team2Score,
        WinnerId = game.WinnerId,
        IsCompleted = game.IsCompleted,
        GameTime = game.GameTime
    };
}
