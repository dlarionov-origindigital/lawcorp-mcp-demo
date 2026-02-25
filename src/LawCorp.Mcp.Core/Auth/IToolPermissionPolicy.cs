namespace LawCorp.Mcp.Core.Auth;

/// <summary>
/// Maps each <see cref="Models.FirmRole"/> to the set of MCP tool names that role
/// is permitted to list and invoke. Applied as a filter on both <c>tools/list</c>
/// (what the caller sees) and <c>tools/call</c> (what the caller can execute).
/// </summary>
public interface IToolPermissionPolicy
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="identity"/>'s role is allowed to
    /// invoke <paramref name="toolName"/>.
    /// </summary>
    bool IsAllowed(string toolName, IFirmIdentityContext identity);

    /// <summary>
    /// Returns the ordered list of tool names accessible to <paramref name="identity"/>'s
    /// role. Used to filter the <c>tools/list</c> response.
    /// </summary>
    IReadOnlyList<string> GetPermittedTools(IFirmIdentityContext identity);
}
