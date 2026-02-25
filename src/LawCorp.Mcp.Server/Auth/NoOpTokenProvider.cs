using LawCorp.Mcp.Core.Auth;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Returns a placeholder token when auth is disabled (UseAuth=false).
/// External API calls will fail with 401, which is the expected behavior —
/// document tools require an authenticated OBO flow.
/// </summary>
public sealed class NoOpTokenProvider : IDownstreamTokenProvider
{
    public Task<string> AcquireTokenAsync(
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult("no-auth-placeholder-token");
    }
}
