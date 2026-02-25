using System.Security.Claims;
using System.Text.Encodings.Web;
using LawCorp.Mcp.Core.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LawCorp.Mcp.ExternalApi.Auth;

/// <summary>
/// Bypasses JWT validation when UseAuth=false for local development.
/// Creates a synthetic ClaimsPrincipal that simulates a Partner-role OBO token,
/// allowing the external API to run without Entra ID configuration.
/// </summary>
public class NoOpAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("oid", "00000000-0000-0000-0000-000000000001"),
            new Claim("name", "Demo User (Partner)"),
            new Claim(ClaimTypes.Role, FirmRole.Partner.ToString()),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
