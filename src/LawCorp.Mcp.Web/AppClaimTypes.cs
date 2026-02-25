namespace LawCorp.Mcp.Web;

/// <summary>
/// Custom claim types from Entra ID tokens not covered by
/// <see cref="System.Security.Claims.ClaimTypes"/>.
/// </summary>
public static class AppClaimTypes
{
    public const string Roles = "roles";
    public const string PreferredUsername = "preferred_username";
}
