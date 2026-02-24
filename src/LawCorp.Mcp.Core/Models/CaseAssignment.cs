namespace LawCorp.Mcp.Core.Models;

public class CaseAssignment
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int AttorneyId { get; set; }
    public AssignmentRole Role { get; set; }
    public DateOnly AssignedDate { get; set; }

    public Case Case { get; set; } = null!;
    public Attorney Attorney { get; set; } = null!;
}
