using System.Security.Claims;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.ExternalApi.Auth;

/// <summary>
/// Extracts user identity from the validated OBO token claims.
/// The external API reads FirmRole from the same role claim values
/// used by the MCP server, but enforces its own authorization.
/// </summary>
public record CallerIdentity(string EntraObjectId, string DisplayName, FirmRole Role)
{
    public static CallerIdentity FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        var oid = principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
            ?? principal.FindFirstValue("oid")
            ?? throw new UnauthorizedAccessException("OBO token missing 'oid' claim.");

        var name = principal.FindFirstValue("name")
            ?? principal.Identity?.Name
            ?? "Unknown";

        var roleClaim = principal.FindFirstValue(ClaimTypes.Role)
            ?? principal.FindFirstValue("roles")
            ?? "Associate";

        var role = Enum.TryParse<FirmRole>(roleClaim, ignoreCase: true, out var parsed)
            ? parsed
            : FirmRole.Associate;

        return new CallerIdentity(oid, name, role);
    }
}
