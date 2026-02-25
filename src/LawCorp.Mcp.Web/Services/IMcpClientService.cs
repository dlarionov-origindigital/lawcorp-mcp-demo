using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LawCorp.Mcp.Web.Services;

public enum McpClientState { Connecting, Ready, Failed }

public interface IMcpClientService
{
    McpClientState State { get; }

    Task<IList<McpClientTool>> ListToolsAsync(CancellationToken ct = default);
    Task<CallToolResult> CallToolAsync(string toolName, Dictionary<string, object?> arguments, CancellationToken ct = default);
}
