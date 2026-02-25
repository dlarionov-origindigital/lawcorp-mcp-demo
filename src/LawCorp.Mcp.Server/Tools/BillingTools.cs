using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class BillingTools
{
    [McpServerTool, Description("Log billable or non-billable time for a user on a case.")]
    public static string TimeEntriesLog(
        [Description("The user ID logging time")] int userId,
        [Description("The case ID to log time against")] int caseId,
        [Description("Number of hours (e.g. 2.5)")] decimal hours,
        [Description("Description of work performed")] string description,
        [Description("Date of the work (ISO 8601 date string)")] string date,
        [Description("Whether this time is billable to the client")] bool billable = true)
        => throw new NotImplementedException("time_entries_log is not yet implemented.");

    [McpServerTool, Description("Search time entries by user, case, date range, and billable status.")]
    public static string TimeEntriesSearch(
        [Description("Filter by user ID")] int? userId = null,
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter from date (ISO 8601 date string)")] string? dateFrom = null,
        [Description("Filter to date (ISO 8601 date string)")] string? dateTo = null,
        [Description("Filter by billable status")] bool? billable = null)
        => throw new NotImplementedException("time_entries_search is not yet implemented.");

    [McpServerTool, Description("Get a billing summary (total hours, billed, outstanding) for a case or client.")]
    public static string BillingGetSummary(
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter by client ID")] int? clientId = null,
        [Description("Summary from date (ISO 8601 date string)")] string? dateFrom = null,
        [Description("Summary to date (ISO 8601 date string)")] string? dateTo = null)
        => throw new NotImplementedException("billing_get_summary is not yet implemented.");

    [McpServerTool, Description("Search invoices by client, status, and date range.")]
    public static string InvoicesSearch(
        [Description("Filter by client ID")] int? clientId = null,
        [Description("Filter by status: Draft, Sent, Paid, Overdue")] string? status = null,
        [Description("Filter from issue date (ISO 8601 date string)")] string? dateFrom = null,
        [Description("Filter to issue date (ISO 8601 date string)")] string? dateTo = null)
        => throw new NotImplementedException("invoices_search is not yet implemented.");

    [McpServerTool, Description("Retrieve full invoice details including all line items.")]
    public static string InvoicesGet(
        [Description("The unique invoice ID")] int invoiceId)
        => throw new NotImplementedException("invoices_get is not yet implemented.");
}
