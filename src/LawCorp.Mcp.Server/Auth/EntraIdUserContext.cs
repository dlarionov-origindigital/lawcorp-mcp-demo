using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Authenticated user context resolved from a validated Entra ID JWT.
/// Created per-request by <see cref="UserContextResolutionMiddleware"/>
/// and stored in <c>HttpContext.Items</c> for consumption by tools and handlers.
/// </summary>
public sealed class EntraIdUserContext : IUserContext
{
    public required int UserId { get; init; }
    public required string DisplayName { get; init; }
    public required AttorneyRole Role { get; init; }
    public bool IsPartner => Role == AttorneyRole.Partner;
    public bool IsAttorney => Role is AttorneyRole.Partner or AttorneyRole.Associate or AttorneyRole.OfCounsel;
}
