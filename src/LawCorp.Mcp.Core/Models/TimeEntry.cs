namespace LawCorp.Mcp.Core.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CaseId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BillableRate { get; set; }
    public bool Billable { get; set; } = true;
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Draft;

    public User User { get; set; } = null!;
    public Case Case { get; set; } = null!;
    public ICollection<InvoiceLineItem> InvoiceLineItems { get; set; } = new List<InvoiceLineItem>();
}
