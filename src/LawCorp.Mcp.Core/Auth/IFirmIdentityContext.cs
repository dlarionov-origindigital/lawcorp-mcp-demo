using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Core.Auth;

/// <summary>
/// Domain-aware identity abstraction resolved from the validated Entra ID JWT.
/// Used by authorization handlers and EF Core query filters to enforce row-level
/// and field-level access without coupling to raw claim strings.
/// </summary>
public interface IFirmIdentityContext
{
    string EntraObjectId { get; }
    int UserId { get; }
    string DisplayName { get; }
    FirmRole FirmRole { get; }
    int? PracticeGroupId { get; }
    IReadOnlyList<int> AssignedCaseIds { get; }
    int? SupervisorId { get; }

    bool HasRole(FirmRole role);
    bool IsInPracticeGroup(int practiceGroupId);
}
