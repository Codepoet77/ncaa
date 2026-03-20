namespace NcaaBracket.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? BracketTitle { get; set; }
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<UserPick> UserPicks { get; set; } = new List<UserPick>();
}
