using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class DocumentTools
{
    [McpServerTool, Description("Search documents by keyword, type, case, and author across the document management system.")]
    public static string DocumentsSearch(
        [Description("Full-text search query")] string query,
        [Description("Filter by type: Motion, Brief, Contract, Correspondence, Evidence")] string? documentType = null,
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter by author user ID")] int? authorId = null,
        int page = 1,
        int pageSize = 20)
        => throw new NotImplementedException("documents_search is not yet implemented.");

    [McpServerTool, Description("Retrieve a document's metadata and full content by document ID.")]
    public static string DocumentsGet(
        [Description("The unique document ID")] int documentId)
        => throw new NotImplementedException("documents_get is not yet implemented.");

    [McpServerTool, Description("Generate a draft document from a standard template, populated with case and client data.")]
    public static string DocumentsDraft(
        [Description("Template type: EngagementLetter, NDA, MergerAgreement, BoardResolution, Motion, Brief")] string templateType,
        [Description("The case ID to associate the document with")] int caseId,
        [Description("JSON object of template-specific parameters")] string parameters)
        => throw new NotImplementedException("documents_draft is not yet implemented.");

    [McpServerTool, Description("Update the status of a document. Valid transitions: Draft → UnderReview → Final → Filed.")]
    public static string DocumentsUpdateStatus(
        [Description("The unique document ID")] int documentId,
        [Description("New status: Draft, UnderReview, Final, or Filed")] string newStatus)
        => throw new NotImplementedException("documents_update_status is not yet implemented.");

    [McpServerTool, Description("List all documents associated with a case, with optional type and status filters.")]
    public static string DocumentsListByCase(
        [Description("The unique case ID")] int caseId,
        [Description("Filter by type: Motion, Brief, Contract, Correspondence, Evidence")] string? documentType = null,
        [Description("Filter by status: Draft, UnderReview, Final, Filed")] string? status = null)
        => throw new NotImplementedException("documents_list_by_case is not yet implemented.");
}
