namespace LawCorp.Mcp.Core.Models;

public class Case
{
    public int Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.Active;
    public int PracticeGroupId { get; set; }
    public int ClientId { get; set; }
    public int? CourtId { get; set; }
    public int? JudgeId { get; set; }
    public DateOnly OpenDate { get; set; }
    public DateOnly? CloseDate { get; set; }
    public decimal EstimatedValue { get; set; }

    public PracticeGroup PracticeGroup { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Court? Court { get; set; }
    public Contact? Judge { get; set; }
    public ICollection<CaseAssignment> Assignments { get; set; } = new List<CaseAssignment>();
    public ICollection<CaseParty> Parties { get; set; } = new List<CaseParty>();
    public ICollection<CaseEvent> Events { get; set; } = new List<CaseEvent>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Hearing> Hearings { get; set; } = new List<Hearing>();
    public ICollection<Deadline> Deadlines { get; set; } = new List<Deadline>();
    public ICollection<ResearchMemo> ResearchMemos { get; set; } = new List<ResearchMemo>();
}
