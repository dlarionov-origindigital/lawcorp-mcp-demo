using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class CaseManagementTools
{
    [McpServerTool, Description("Search cases by keyword, status, practice group, assigned attorney, and date range.")]
    public static string CasesSearch(
        [Description("Full-text search query")] string query,
        [Description("Filter by status: Active, OnHold, Closed, Settled")] string? status = null,
        [Description("Filter by practice group name")] string? practiceGroup = null,
        [Description("Filter by assigned attorney ID")] int? assignedTo = null,
        [Description("Filter from date (ISO 8601 date string)")] string? dateFrom = null,
        [Description("Filter to date (ISO 8601 date string)")] string? dateTo = null,
        int page = 1,
        int pageSize = 20)
        => throw new NotImplementedException("cases_search is not yet implemented.");

    [McpServerTool, Description("Retrieve full case details by case ID, including team, parties, status, and key dates.")]
    public static string CasesGet(
        [Description("The unique case ID")] int caseId)
        => throw new NotImplementedException("cases_get is not yet implemented.");

    [McpServerTool, Description("Update the status of a case. Valid transitions: Active → OnHold, Active → Closed, Active → Settled.")]
    public static string CasesUpdateStatus(
        [Description("The unique case ID")] int caseId,
        [Description("New status: Active, OnHold, Closed, or Settled")] string newStatus,
        [Description("Optional reason for the status change")] string? reason = null)
        => throw new NotImplementedException("cases_update_status is not yet implemented.");

    [McpServerTool, Description("Assign or reassign an attorney to a case with a specified role. Partner-only action.")]
    public static string CasesAssignAttorney(
        [Description("The unique case ID")] int caseId,
        [Description("The attorney ID to assign")] int attorneyId,
        [Description("Role on the case: Lead, Supporting, or Reviewer")] string role)
        => throw new NotImplementedException("cases_assign_attorney is not yet implemented.");

    [McpServerTool, Description("Retrieve a chronological timeline of all events for a case.")]
    public static string CasesGetTimeline(
        [Description("The unique case ID")] int caseId,
        [Description("Optional filter by event type: StatusChange, Assignment, Note, Filing, Hearing, etc.")] string? eventType = null)
        => throw new NotImplementedException("cases_get_timeline is not yet implemented.");

    [McpServerTool, Description("Add a note or comment to a case record.")]
    public static string CasesAddNote(
        [Description("The unique case ID")] int caseId,
        [Description("The note content")] string content,
        [Description("Mark as attorney-client privileged (not visible to paralegals or interns)")] bool isPrivileged = false)
        => throw new NotImplementedException("cases_add_note is not yet implemented.");
}
