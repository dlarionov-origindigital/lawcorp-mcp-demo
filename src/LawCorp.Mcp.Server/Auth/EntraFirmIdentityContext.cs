using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

public sealed class EntraFirmIdentityContext : IFirmIdentityContext
{
    public required string EntraObjectId { get; init; }
    public required int UserId { get; init; }
    public required string DisplayName { get; init; }
    public FirmRole FirmRole { get; init; }
    public int? PracticeGroupId { get; init; }
    public required IReadOnlyList<int> AssignedCaseIds { get; init; }
    public int? SupervisorId { get; init; }

    public bool HasRole(FirmRole role) => FirmRole == role;
    public bool IsInPracticeGroup(int practiceGroupId) => PracticeGroupId == practiceGroupId;
}
