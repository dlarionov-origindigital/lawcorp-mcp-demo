using LawCorp.Mcp.Core.Auth;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// MCP request filters that enforce <see cref="IToolPermissionPolicy"/> at the
/// protocol boundary. Registered via <c>.WithRequestFilters()</c> in ServerBootstrap.
/// <para>
/// When <see cref="IFirmIdentityContext"/> is not resolvable (anonymous / stdio mode),
/// both filters pass through unchanged — full access is preserved for dev/demo use.
/// </para>
/// </summary>
public static class ToolPermissionFilters
{
    /// <summary>
    /// Filters the <c>tools/list</c> response so each caller only sees the tools
    /// their role permits. The filter runs after the default handler populates the
    /// full tool roster, then removes any tools not in the caller's permitted set.
    /// </summary>
    public static McpRequestFilter<ListToolsRequestParams, ListToolsResult> ListTools =>
        next => async (context, ct) =>
        {
            var result = await next(context, ct);

            var identity = context.Services?.GetService<IFirmIdentityContext>();
            if (identity is null)
                return result; // anonymous / stdio mode — pass through

            var policy = context.Services!.GetRequiredService<IToolPermissionPolicy>();
            var permitted = new HashSet<string>(
                policy.GetPermittedTools(identity),
                StringComparer.OrdinalIgnoreCase);

            var filtered = result.Tools.Where(t => permitted.Contains(t.Name)).ToList();
            result.Tools.Clear();
            foreach (var tool in filtered)
                result.Tools.Add(tool);

            return result;
        };

    /// <summary>
    /// Guards <c>tools/call</c> by checking the requested tool name against the
    /// caller's permitted set before the tool handler executes. Returns a structured
    /// MCP error result (not an exception) when access is denied.
    /// </summary>
    public static McpRequestFilter<CallToolRequestParams, CallToolResult> CallTool =>
        next => async (context, ct) =>
        {
            var identity = context.Services?.GetService<IFirmIdentityContext>();
            if (identity is null)
                return await next(context, ct); // anonymous / stdio mode — pass through

            var toolName = context.Params?.Name;
            if (toolName is not null)
            {
                var policy = context.Services!.GetRequiredService<IToolPermissionPolicy>();
                if (!policy.IsAllowed(toolName, identity))
                {
                    return new CallToolResult
                    {
                        Content =
                        [
                            new TextContentBlock
                            {
                                Text = $"Access denied: the '{identity.FirmRole}' role is not permitted to call '{toolName}'."
                            }
                        ],
                        IsError = true,
                    };
                }
            }

            return await next(context, ct);
        };
}
