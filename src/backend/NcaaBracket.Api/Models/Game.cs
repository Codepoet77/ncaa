namespace NcaaBracket.Api.Models;

public class Game
{
    public int Id { get; set; }
    public string? EspnId { get; set; }
    public int Round { get; set; }
    public string? Region { get; set; }
    public int BracketPosition { get; set; }
    public int? Team1Id { get; set; }
    public int? Team2Id { get; set; }
    public int? WinnerId { get; set; }
    public int? Team1Score { get; set; }
    public int? Team2Score { get; set; }
    public DateTime? GameTime { get; set; }
    public bool IsCompleted { get; set; }
    public int? NextGameId { get; set; }
    public int? Slot { get; set; }

    public Team? Team1 { get; set; }
    public Team? Team2 { get; set; }
    public Team? Winner { get; set; }
    public Game? NextGame { get; set; }
}
