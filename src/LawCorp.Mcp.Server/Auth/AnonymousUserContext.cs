using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Demo user context used when UseAuth=false in appsettings.
/// Acts as a Partner with full access — all role checks pass, no Entra ID required.
/// </summary>
public class AnonymousUserContext : IUserContext
{
    public int UserId => 1;
    public string DisplayName => "Demo Partner";
    public FirmRole Role => FirmRole.Partner;
    public bool IsPartner => true;
    public bool IsAttorney => true;
}
