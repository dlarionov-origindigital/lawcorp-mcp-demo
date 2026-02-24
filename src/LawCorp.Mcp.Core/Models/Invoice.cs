namespace LawCorp.Mcp.Core.Models;

public class Invoice
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int CaseId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string Notes { get; set; } = string.Empty;

    public Client Client { get; set; } = null!;
    public Case Case { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}
