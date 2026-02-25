namespace LawCorp.Mcp.Core.Models;

/// <summary>
/// Satellite table for attorney-specific attributes. Linked 1:1 to <see cref="User"/>
/// via <see cref="UserId"/>. Only populated when <c>User.FirmRole</c> is
/// Partner, Associate, or OfCounsel.
/// </summary>
public class AttorneyDetails
{
    public int UserId { get; set; }
    public string BarNumber { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }

    public User User { get; set; } = null!;
}
