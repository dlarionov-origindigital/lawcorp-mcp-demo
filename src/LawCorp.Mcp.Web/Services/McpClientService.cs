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

    private IMcpClient? _client;

    public McpClientState State { get; private set; } = McpClientState.Connecting;

    public McpClientService(IConfiguration config, IServiceProvider services)
    {
        _config = config;
        _services = services;
    }

    private async Task<IMcpClient> EnsureClientAsync(CancellationToken ct)
    {
        if (_client is not null && State == McpClientState.Ready)
            return _client;

        await _lock.WaitAsync(ct);
        try
        {
            if (_client is not null && State == McpClientState.Ready)
                return _client;

            var endpoint = _config["McpServer:Endpoint"] ?? "http://localhost:5000/mcp";
            var httpClient = new HttpClient { BaseAddress = new Uri(endpoint) };

            // 8.2.2: attach bearer token when auth is enabled
            if (_config.GetValue<bool>("UseAuth"))
            {
                var tokenAcquisition = _services.GetService<ITokenAcquisition>();
                if (tokenAcquisition is not null)
                {
                    var scopes = _config.GetSection("McpServer:Scopes").Get<string[]>() ?? [];
                    var token = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }
            }

            var transport = new SseClientTransport(
                new SseClientTransportOptions { Endpoint = new Uri(endpoint) },
                httpClient);

            _client = await McpClientFactory.CreateAsync(transport, cancellationToken: ct);
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

    public async Task<CallToolResponse> CallToolAsync(
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
