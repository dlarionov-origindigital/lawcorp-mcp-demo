using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Production implementation of <see cref="IFirmIdentityContext"/> resolved from
/// the validated Entra ID JWT and the local database. Created per-request by
/// <see cref="UserContextResolutionMiddleware"/>.
/// </summary>
public sealed class EntraFirmIdentityContext : IFirmIdentityContext
{
    public required string EntraObjectId { get; init; }
    public required int UserId { get; init; }
    public required string DisplayName { get; init; }
    public AttorneyRole? AttorneyRole { get; init; }
    public int? PracticeGroupId { get; init; }
    public required IReadOnlyList<int> AssignedCaseIds { get; init; }
    public int? AssignedAttorneyId { get; init; }

    public bool HasRole(AttorneyRole role) => AttorneyRole == role;
    public bool IsInPracticeGroup(int practiceGroupId) => PracticeGroupId == practiceGroupId;
}
