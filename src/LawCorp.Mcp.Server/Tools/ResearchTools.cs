using LawCorp.Mcp.Core;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class ResearchTools
{
    [McpServerTool(Name = McpToolName.Research.SearchPrecedents), Description("Search case law precedents by topic, jurisdiction, date range, and practice area.")]
    public static string ResearchSearchPrecedents(
        [Description("Full-text search query (topic, case name, principle)")] string query,
        [Description("Filter by jurisdiction (e.g., Delaware, Federal/SDNY)")] string? jurisdiction = null,
        [Description("Precedents from date (ISO 8601 date string)")] string? dateFrom = null,
        [Description("Precedents to date (ISO 8601 date string)")] string? dateTo = null,
        [Description("Filter by practice area (e.g., M&A, Corporate Governance)")] string? practiceArea = null)
        => throw new NotImplementedException("research_search_precedents is not yet implemented.");

    [McpServerTool(Name = McpToolName.Research.GetStatute), Description("Retrieve the text and annotations for a specific statute by ID and jurisdiction.")]
    public static string ResearchGetStatute(
        [Description("The statute identifier (e.g., 'DGCL-141')")] string statuteId,
        [Description("The jurisdiction (e.g., Delaware, Federal)")] string jurisdiction)
        => throw new NotImplementedException("research_get_statute is not yet implemented.");

    [McpServerTool(Name = McpToolName.Research.GetMemo), Description("Retrieve a research memo by ID, including full findings and tags.")]
    public static string ResearchGetMemo(
        [Description("The unique research memo ID")] int memoId)
        => throw new NotImplementedException("research_get_memo is not yet implemented.");

    [McpServerTool(Name = McpToolName.Research.CreateMemo), Description("Create a new research memo linked to a case.")]
    public static string ResearchCreateMemo(
        [Description("The case ID to associate the memo with")] int caseId,
        [Description("The legal topic of the memo")] string topic,
        [Description("The research findings and analysis")] string findings,
        [Description("The user ID authoring the memo")] int authorId)
        => throw new NotImplementedException("research_create_memo is not yet implemented.");

    [McpServerTool(Name = McpToolName.Research.SearchMemos), Description("Search existing research memos by topic, case, or author.")]
    public static string ResearchSearchMemos(
        [Description("Full-text search query")] string query,
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter by author user ID")] int? authorId = null,
        int page = 1,
        int pageSize = 20)
        => throw new NotImplementedException("research_search_memos is not yet implemented.");
}
