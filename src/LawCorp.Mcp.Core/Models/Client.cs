namespace LawCorp.Mcp.Core.Models;

public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClientType Type { get; set; }
    public string Industry { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateOnly EngagementDate { get; set; }
    public string Status { get; set; } = "Active";

    public ICollection<Case> Cases { get; set; } = new List<Case>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
