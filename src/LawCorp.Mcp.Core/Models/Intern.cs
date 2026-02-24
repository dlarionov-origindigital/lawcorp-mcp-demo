namespace LawCorp.Mcp.Core.Models;

public class Intern
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public int PracticeGroupId { get; set; }
    public int SupervisorId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? EntraObjectId { get; set; }

    public PracticeGroup PracticeGroup { get; set; } = null!;
    public Attorney Supervisor { get; set; } = null!;
}
