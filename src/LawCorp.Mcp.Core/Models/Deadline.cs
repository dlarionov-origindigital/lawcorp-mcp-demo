namespace LawCorp.Mcp.Core.Models;

public class Deadline
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DeadlineUrgency Urgency { get; set; } = DeadlineUrgency.Normal;
    public DeadlineType Type { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public int? AssignedToId { get; set; }

    public Case Case { get; set; } = null!;
    public Attorney? AssignedTo { get; set; }
}
