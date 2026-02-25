using LawCorp.Mcp.Web;
using Microsoft.Identity.Web;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net.Http.Headers;

namespace LawCorp.Mcp.Web.Services;

/// <summary>
/// Scoped MCP client — one instance per Blazor Server circuit.
/// Lazily establishes an HttpClientTransport connection to the MCP server on first call,
/// injecting the authenticated user's bearer token when UseAuth is enabled (8.2.2).
/// </summary>
public sealed class McpClientService : IMcpClientService, IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private McpClient? _client;

    public McpClientState State { get; private set; } = McpClientState.Connecting;

    public McpClientService(IConfiguration config, IServiceProvider services)
    {
        _config = config;
        _services = services;
    }

    private async Task<McpClient> EnsureClientAsync(CancellationToken ct)
    {
        if (_client is not null && State == McpClientState.Ready)
            return _client;

        await _lock.WaitAsync(ct);
        try
        {
            if (_client is not null && State == McpClientState.Ready)
                return _client;

            var endpoint = _config[AppConfigKeys.McpServer.Endpoint] ?? AppConfigKeys.McpServer.DefaultEndpoint;
            var httpClient = new HttpClient();

            // 8.2.2: attach bearer token when auth is enabled
            if (_config.GetValue<bool>(AppConfigKeys.UseAuth))
            {
                var tokenAcquisition = _services.GetService<ITokenAcquisition>();
                if (tokenAcquisition is not null)
                {
                    var scopes = _config.GetSection(AppConfigKeys.McpServer.Scopes).Get<string[]>() ?? [];
                    var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(endpoint) },
                httpClient,
                ownsHttpClient: true);

            _client = await McpClient.CreateAsync(transport, cancellationToken: ct);
            State = McpClientState.Ready;
            return _client;
        }
        catch
        {
            State = McpClientState.Failed;
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IList<McpClientTool>> ListToolsAsync(CancellationToken ct = default)
    {
        var client = await EnsureClientAsync(ct);
        return await client.ListToolsAsync(cancellationToken: ct);
    }

    public async Task<CallToolResult> CallToolAsync(
        string toolName,
        Dictionary<string, object?> arguments,
        CancellationToken ct = default)
    {
        var client = await EnsureClientAsync(ct);
        return await client.CallToolAsync(toolName, arguments, cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
        _lock.Dispose();
    }
}
