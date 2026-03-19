namespace NcaaBracket.Api.Models;

public class UserPick
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int GameId { get; set; }
    public int PickedTeamId { get; set; }
    public bool? IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Game Game { get; set; } = null!;
    public Team PickedTeam { get; set; } = null!;
}
