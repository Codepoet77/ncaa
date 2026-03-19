namespace NcaaBracket.Api.DTOs;

public class SubmitPicksRequest
{
    public List<PickItem> Picks { get; set; } = new();
}

public class PickItem
{
    public int GameId { get; set; }
    public int PickedTeamId { get; set; }
}

public class UpdateBracketTitleRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UserPickDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int PickedTeamId { get; set; }
    public string? PickedTeamName { get; set; }
    public bool? IsCorrect { get; set; }
    public int PointsEarned { get; set; }
}
