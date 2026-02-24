namespace LawCorp.Mcp.Core.Auth;

/// <summary>
/// Acquires a delegated access token for a downstream API on behalf of the current user.
/// Production implementation uses MSAL's On-Behalf-Of (OBO) flow;
/// test doubles return a static token.
/// </summary>
public interface IDownstreamTokenProvider
{
    /// <summary>
    /// Exchanges the current user's inbound token for a downstream-scoped token
    /// via the OBO flow.
    /// </summary>
    /// <param name="scopes">
    /// The scopes required by the downstream API
    /// (e.g. <c>["https://graph.microsoft.com/User.Read"]</c>).
    /// </param>
    Task<string> AcquireTokenAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default);
}
