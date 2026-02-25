namespace LawCorp.Mcp.Core.Models;

/// <summary>
/// Satellite table for intern-specific attributes. Linked 1:1 to <see cref="User"/>
/// via <see cref="UserId"/>. Only populated when <c>User.FirmRole</c> is Intern.
/// </summary>
public class InternDetails
{
    public int UserId { get; set; }
    public string School { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public User User { get; set; } = null!;
}
