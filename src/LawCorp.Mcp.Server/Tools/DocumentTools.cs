using LawCorp.Mcp.Core;
using LawCorp.Mcp.Core.Queries;
using MediatR;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LawCorp.Mcp.Server.Tools;

/// <summary>
/// Document management tools dispatched through MediatR.
/// Handlers make network calls to the external DMS API via OBO token exchange.
/// The tools themselves are unaware of the network boundary.
/// </summary>
[McpServerToolType]
public class DocumentTools(IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [McpServerTool(Name = McpToolName.Documents.Search), Description(
        "Search documents by keyword, type, case, and author across the document management system.")]
    public async Task<string> DocumentsSearch(
        [Description("Full-text search query")] string query,
        [Description("Filter by type: Motion, Brief, Contract, Correspondence, Evidence")] string? documentType = null,
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter by author user ID")] int? authorId = null,
        [Description("Page number, 1-based")] int page = 1,
        [Description("Results per page (max 100)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new SearchDocumentsQuery(query, documentType, caseId, authorId, page, pageSize), ct);

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = McpToolName.Documents.Get), Description(
        "Retrieve a document's metadata and full content by document ID.")]
    public async Task<string> DocumentsGet(
        [Description("The unique document ID")] int documentId,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetDocumentByIdQuery(documentId), ct);

        if (result.Error is not null)
            return JsonSerializer.Serialize(new { error = result.Error }, JsonOpts);

        return JsonSerializer.Serialize(result.Document, JsonOpts);
    }

    [McpServerTool(Name = McpToolName.Documents.ListByCase), Description(
        "List all documents associated with a case, with optional type and status filters.")]
    public async Task<string> DocumentsListByCase(
        [Description("The unique case ID")] int caseId,
        [Description("Filter by type: Motion, Brief, Contract, Correspondence, Evidence")] string? documentType = null,
        [Description("Filter by status: Draft, UnderReview, Final, Filed")] string? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new ListDocumentsByCaseQuery(caseId, documentType, status), ct);

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = McpToolName.Documents.Draft), Description(
        "Generate a draft document from a standard template, populated with case and client data.")]
    public static string DocumentsDraft(
        [Description("Template type: EngagementLetter, NDA, MergerAgreement, BoardResolution, Motion, Brief")] string templateType,
        [Description("The case ID to associate the document with")] int caseId,
        [Description("JSON object of template-specific parameters")] string parameters)
        => throw new NotImplementedException("documents_draft — pending MediatR migration.");

    [McpServerTool(Name = McpToolName.Documents.UpdateStatus), Description(
        "Update the status of a document. Valid transitions: Draft → UnderReview → Final → Filed.")]
    public static string DocumentsUpdateStatus(
        [Description("The unique document ID")] int documentId,
        [Description("New status: Draft, UnderReview, Final, or Filed")] string newStatus)
        => throw new NotImplementedException("documents_update_status — pending MediatR migration.");
}
