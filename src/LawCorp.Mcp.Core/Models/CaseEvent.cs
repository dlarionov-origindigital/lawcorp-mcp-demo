namespace LawCorp.Mcp.Core.Models;

public class CaseEvent
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public CaseEventType EventType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CreatedById { get; set; }

    public Case Case { get; set; } = null!;
}
