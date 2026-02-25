using LawCorp.Mcp.Core;
using LawCorp.Mcp.Core.Commands;
using LawCorp.Mcp.Core.Queries;
using MediatR;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LawCorp.Mcp.Server.Tools;

/// <summary>
/// Case management tools dispatched through MediatR.
/// Handlers resolve data from the local database (in-runtime via DbContext).
/// </summary>
[McpServerToolType]
public class CaseManagementTools(IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [McpServerTool(Name = McpToolName.Cases.Search), Description(
        "Search cases by keyword, status, practice group, assigned attorney, and date range. " +
        "Returns paginated results with case summaries.")]
    public async Task<string> CasesSearch(
        [Description("Full-text search across case title, description, and case number")] string query,
        [Description("Filter by status: Active, OnHold, Closed, or Settled")] string? status = null,
        [Description("Filter by practice group name (partial match)")] string? practiceGroup = null,
        [Description("Filter by assigned attorney ID")] int? assignedTo = null,
        [Description("Return only cases opened on or after this date (yyyy-MM-dd)")] string? dateFrom = null,
        [Description("Return only cases opened on or before this date (yyyy-MM-dd)")] string? dateTo = null,
        [Description("Page number, 1-based")] int page = 1,
        [Description("Results per page (max 100)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new SearchCasesQuery(
            query, status, practiceGroup, assignedTo, dateFrom, dateTo, page, pageSize), ct);

        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = McpToolName.Cases.Get), Description(
        "Retrieve full case details by case ID, including assigned team, parties, court, and key dates.")]
    public async Task<string> CasesGet(
        [Description("The unique case ID")] int caseId,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCaseByIdQuery(caseId), ct);

        if (result.Error is not null)
            return JsonSerializer.Serialize(new { error = result.Error }, JsonOpts);

        return JsonSerializer.Serialize(result.Case, JsonOpts);
    }

    [McpServerTool(Name = McpToolName.Cases.UpdateStatus), Description(
        "Update the status of a case. " +
        "Valid transitions: Active → OnHold | Closed | Settled; OnHold → Active. " +
        "Requires lead attorney or partner role.")]
    public async Task<string> CasesUpdateStatus(
        [Description("The unique case ID")] int caseId,
        [Description("New status: Active, OnHold, Closed, or Settled")] string newStatus,
        [Description("Optional reason for the status change (recorded in case timeline)")] string? reason = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new UpdateCaseStatusCommand(caseId, newStatus, reason), ct);

        if (!result.Success)
            return JsonSerializer.Serialize(new { error = result.Error }, JsonOpts);

        return JsonSerializer.Serialize(new
        {
            success = true,
            result.Data,
            result.Message
        }, JsonOpts);
    }

    [McpServerTool(Name = McpToolName.Cases.AssignUser), Description(
        "Assign or reassign a user to a case with a specified role. Partner-only action.")]
    public static string CasesAssignUser(
        [Description("The unique case ID")] int caseId,
        [Description("The user ID to assign")] int userId,
        [Description("Role on the case: Lead, Supporting, or Reviewer")] string role)
        => throw new NotImplementedException("cases_assign_user — pending MediatR migration.");

    [McpServerTool(Name = McpToolName.Cases.GetTimeline), Description(
        "Retrieve a chronological timeline of all events for a case. " +
        "Optionally filter by event type. Privileged notes are hidden from non-attorneys.")]
    public static string CasesGetTimeline(
        [Description("The unique case ID")] int caseId,
        [Description("Filter by event type: StatusChange, Assignment, Note, Filing, Hearing, Deadline, DocumentAdded, Other")] string? eventType = null)
        => throw new NotImplementedException("cases_get_timeline — pending MediatR migration.");

    [McpServerTool(Name = McpToolName.Cases.AddNote), Description(
        "Add a note or comment to a case record. " +
        "Privileged notes are visible only to attorneys (not paralegals or interns).")]
    public static string CasesAddNote(
        [Description("The unique case ID")] int caseId,
        [Description("The note content")] string content,
        [Description("Mark as attorney-client privileged")] bool isPrivileged = false)
        => throw new NotImplementedException("cases_add_note — pending MediatR migration.");
}
