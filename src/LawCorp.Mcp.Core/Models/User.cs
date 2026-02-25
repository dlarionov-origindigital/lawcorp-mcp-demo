namespace LawCorp.Mcp.Core.Models;

/// <summary>
/// Canonical identity record for all firm personnel. Every person who logs in
/// (attorney, paralegal, legal assistant, intern) has exactly one row here.
/// Role-specific attributes live in satellite tables (<see cref="AttorneyDetails"/>,
/// <see cref="InternDetails"/>).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Entra ID Object ID (<c>oid</c> claim). Nullable for backwards compatibility
    /// with demo/seed data. Used to resolve the user from an inbound JWT.
    /// </summary>
    public string? EntraObjectId { get; set; }

    public FirmRole FirmRole { get; set; }
    public int? PracticeGroupId { get; set; }
    public DateOnly HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// For roles that report to another user (LegalAssistant, Intern).
    /// Null for attorneys and paralegals.
    /// </summary>
    public int? SupervisorId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────

    public PracticeGroup? PracticeGroup { get; set; }
    public User? Supervisor { get; set; }
    public AttorneyDetails? AttorneyDetails { get; set; }
    public InternDetails? InternDetails { get; set; }

    public ICollection<CaseAssignment> CaseAssignments { get; set; } = new List<CaseAssignment>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    public ICollection<Document> AuthoredDocuments { get; set; } = new List<Document>();
}
