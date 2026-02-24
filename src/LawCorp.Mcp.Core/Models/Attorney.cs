namespace LawCorp.Mcp.Core.Models;

public class Attorney
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BarNumber { get; set; } = string.Empty;
    public AttorneyRole Role { get; set; }
    public int PracticeGroupId { get; set; }
    public decimal HourlyRate { get; set; }
    public DateOnly HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Entra ID Object ID (<c>oid</c> claim). Nullable for backwards compatibility
    /// with demo/seed data. When populated, used to resolve the attorney from an
    /// inbound JWT during authenticated requests.
    /// </summary>
    public string? EntraObjectId { get; set; }

    public PracticeGroup PracticeGroup { get; set; } = null!;
    public ICollection<CaseAssignment> CaseAssignments { get; set; } = new List<CaseAssignment>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<Document> AuthoredDocuments { get; set; } = new List<Document>();
}
