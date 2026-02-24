using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Core.Auth;

/// <summary>
/// Domain-aware identity abstraction resolved from the validated Entra ID JWT.
/// Used by authorization handlers and EF Core query filters to enforce row-level
/// and field-level access without coupling to raw claim strings.
/// <para>
/// Registered as <b>scoped</b> — each request gets its own resolved identity.
/// See <see cref="IUserContext"/> for the lightweight subset used by tools.
/// </para>
/// </summary>
public interface IFirmIdentityContext
{
    /// <summary>Entra ID Object ID (<c>oid</c> claim) — the immutable user identifier in Azure AD.</summary>
    string EntraObjectId { get; }

    /// <summary>Database primary key for the attorney/staff record.</summary>
    int UserId { get; }

    /// <summary>Full display name for audit and attribution.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Attorney role if the user is an attorney; <c>null</c> for non-attorney staff
    /// (Paralegal, LegalAssistant, Intern).
    /// </summary>
    AttorneyRole? AttorneyRole { get; }

    /// <summary>Practice group the user belongs to, if applicable.</summary>
    int? PracticeGroupId { get; }

    /// <summary>Database IDs of cases assigned to this user (for row-level filtering).</summary>
    IReadOnlyList<int> AssignedCaseIds { get; }

    /// <summary>
    /// For LegalAssistant and Intern personas, the attorney they are assigned to.
    /// <c>null</c> for attorneys and other staff.
    /// </summary>
    int? AssignedAttorneyId { get; }

    bool HasRole(AttorneyRole role);
    bool IsInPracticeGroup(int practiceGroupId);
}
