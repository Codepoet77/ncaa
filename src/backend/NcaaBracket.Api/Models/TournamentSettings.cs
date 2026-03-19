namespace NcaaBracket.Api.Models;

public class TournamentSettings
{
    public int Id { get; set; }
    public int Year { get; set; }
    public DateTime LockDate { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastEspnSync { get; set; }
}
