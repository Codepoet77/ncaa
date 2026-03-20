namespace NcaaBracket.Api.DTOs;

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? BracketTitle { get; set; }
    public int TotalPoints { get; set; }
    public int MaxPossiblePoints { get; set; }
    public int CorrectPicks { get; set; }
    public DateTime? LastPickAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
