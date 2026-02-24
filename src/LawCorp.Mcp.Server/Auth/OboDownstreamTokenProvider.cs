using LawCorp.Mcp.Core.Auth;
using Microsoft.Identity.Web;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Acquires downstream-scoped tokens via MSAL's On-Behalf-Of (OBO) flow.
/// The inbound user token (validated by JWT Bearer middleware) is exchanged for
/// a token scoped to the requested downstream API (e.g. Microsoft Graph).
/// <para>
/// Token caching is handled by Microsoft.Identity.Web's in-memory cache (dev) or
/// distributed cache (production). See <c>AddInMemoryTokenCaches()</c> in
/// <see cref="AuthServiceCollectionExtensions"/>.
/// </para>
/// </summary>
public sealed class OboDownstreamTokenProvider : IDownstreamTokenProvider
{
    private readonly ITokenAcquisition _tokenAcquisition;

    public OboDownstreamTokenProvider(ITokenAcquisition tokenAcquisition)
    {
        _tokenAcquisition = tokenAcquisition;
    }

    public async Task<string> AcquireTokenAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(
                scopes,
                tokenAcquisitionOptions: new TokenAcquisitionOptions
                {
                    CancellationToken = cancellationToken
                });
        }
        catch (MicrosoftIdentityWebChallengeUserException ex)
        {
            throw new InvalidOperationException(
                $"User consent is required for the requested scopes. " +
                $"Direct the user to the consent URL. Inner: {ex.Message}", ex);
        }
    }
}
