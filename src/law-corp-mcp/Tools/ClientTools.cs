using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class ClientTools
{
    [McpServerTool, Description("Search clients by name, industry, or type (Individual or Organization).")]
    public static string ClientsSearch(
        [Description("Full-text search query")] string query,
        [Description("Filter by client type: Individual or Organization")] string? clientType = null,
        [Description("Filter by industry (e.g., Technology, Finance, Healthcare)")] string? industry = null)
        => throw new NotImplementedException("clients_search is not yet implemented.");

    [McpServerTool, Description("Retrieve a client's full profile and engagement history, including all associated cases.")]
    public static string ClientsGet(
        [Description("The unique client ID")] int clientId)
        => throw new NotImplementedException("clients_get is not yet implemented.");

    [McpServerTool, Description("Run a conflict-of-interest check for a potential new client or matter against existing firm relationships.")]
    public static string ClientsConflictCheck(
        [Description("Name of the prospective client")] string clientName,
        [Description("JSON array of opposing party names")] string opposingParties,
        [Description("JSON array of related entity names (subsidiaries, affiliates)")] string? relatedEntities = null)
        => throw new NotImplementedException("clients_conflict_check is not yet implemented.");

    [McpServerTool, Description("Search contacts such as witnesses, expert witnesses, judges, and opposing counsel.")]
    public static string ContactsSearch(
        [Description("Full-text search query")] string query,
        [Description("Filter by type: Witness, Expert, Judge, OpposingCounsel")] string? contactType = null,
        [Description("Filter by jurisdiction")] string? jurisdiction = null)
        => throw new NotImplementedException("contacts_search is not yet implemented.");

    [McpServerTool, Description("Retrieve full contact details by contact ID.")]
    public static string ContactsGet(
        [Description("The unique contact ID")] int contactId)
        => throw new NotImplementedException("contacts_get is not yet implemented.");
}
