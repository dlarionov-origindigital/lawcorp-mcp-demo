namespace LawCorp.Mcp.Core.Models;

public class InvoiceLineItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? TimeEntryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Hours { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public TimeEntry? TimeEntry { get; set; }
}
