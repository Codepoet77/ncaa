namespace NcaaBracket.Api.DTOs;

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public int Seed { get; set; }
    public string Region { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public class GameDto
{
    public int Id { get; set; }
    public int Round { get; set; }
    public string? Region { get; set; }
    public int BracketPosition { get; set; }
    public int? NextGameId { get; set; }
    public int? Slot { get; set; }
    public TeamDto? Team1 { get; set; }
    public TeamDto? Team2 { get; set; }
    public int? Team1Score { get; set; }
    public int? Team2Score { get; set; }
    public int? WinnerId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? GameTime { get; set; }
}

public class BracketResponse
{
    public Dictionary<string, Dictionary<int, List<GameDto>>> Regions { get; set; } = new();
}
