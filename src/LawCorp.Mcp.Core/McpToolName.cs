namespace LawCorp.Mcp.Core;

/// <summary>
/// Canonical MCP tool name constants for the Law-Corp server, grouped by domain.
/// <para>
/// These are the authoritative string identifiers used:
/// <list type="bullet">
///   <item>On <c>[McpServerTool(Name = McpToolName.X.Y)]</c> to register tools with the MCP SDK</item>
///   <item>In <c>ToolPermissionMatrix</c> to define per-role access</item>
///   <item>In any code that references tools by name (filters, audit logs, tests)</item>
/// </list>
/// Define each tool name once here. Never write a raw tool name string literal elsewhere.
/// </para>
/// </summary>
public static class McpToolName
{
    public static class Cases
    {
        public const string Search = "cases_search";
        public const string Get = "cases_get";
        public const string UpdateStatus = "cases_update_status";
        public const string AssignUser = "cases_assign_user";
        public const string GetTimeline = "cases_get_timeline";
        public const string AddNote = "cases_add_note";
    }

    public static class Documents
    {
        public const string Search = "documents_search";
        public const string Get = "documents_get";
        public const string Draft = "documents_draft";
        public const string UpdateStatus = "documents_update_status";
        public const string ListByCase = "documents_list_by_case";
    }

    public static class Clients
    {
        public const string Search = "clients_search";
        public const string Get = "clients_get";
        public const string ConflictCheck = "clients_conflict_check";
    }

    public static class Contacts
    {
        public const string Search = "contacts_search";
        public const string Get = "contacts_get";
    }

    public static class Billing
    {
        public const string TimeEntriesLog = "time_entries_log";
        public const string TimeEntriesSearch = "time_entries_search";
        public const string GetSummary = "billing_get_summary";
        public const string InvoicesSearch = "invoices_search";
        public const string InvoicesGet = "invoices_get";
    }

    public static class Calendar
    {
        public const string GetHearings = "calendar_get_hearings";
        public const string GetDeadlines = "calendar_get_deadlines";
        public const string AddEvent = "calendar_add_event";
        public const string GetConflicts = "calendar_get_conflicts";
    }

    public static class Research
    {
        public const string SearchPrecedents = "research_search_precedents";
        public const string GetStatute = "research_get_statute";
        public const string GetMemo = "research_get_memo";
        public const string CreateMemo = "research_create_memo";
        public const string SearchMemos = "research_search_memos";
    }

    public static class Intake
    {
        public const string CreateRequest = "intake_create_request";
        public const string GetRequest = "intake_get_request";
        public const string RunConflictCheck = "intake_run_conflict_check";
        public const string Approve = "intake_approve";
        public const string GenerateEngagementLetter = "intake_generate_engagement_letter";
    }
}
