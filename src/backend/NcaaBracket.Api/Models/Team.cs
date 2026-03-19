namespace NcaaBracket.Api.Models;

public class Team
{
    public int Id { get; set; }
    public string? EspnId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Seed { get; set; }
    public string Region { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? ShortName { get; set; }
}
