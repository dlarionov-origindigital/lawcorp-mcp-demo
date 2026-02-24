using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Demo user context used when UseAuth=false in appsettings.
/// Acts as a Partner with full access — all role checks pass, no Entra ID required.
/// Replace with an Entra ID implementation when auth is implemented (Epic 1.2).
/// </summary>
public class AnonymousUserContext : IUserContext
{
    public int UserId => 1;
    public string DisplayName => "Demo Partner";
    public AttorneyRole Role => AttorneyRole.Partner;
    public bool IsPartner => true;
    public bool IsAttorney => true;
}
