using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public static class CalendarTools
{
    [McpServerTool, Description("Get upcoming court hearings for a case or attorney within a date range.")]
    public static string CalendarGetHearings(
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter by user ID")] int? userId = null,
        [Description("Start of date range (ISO 8601 date string)")] string? dateFrom = null,
        [Description("End of date range (ISO 8601 date string)")] string? dateTo = null)
        => throw new NotImplementedException("calendar_get_hearings is not yet implemented.");

    [McpServerTool, Description("Get filing deadlines and statute of limitations dates, optionally filtered by urgency.")]
    public static string CalendarGetDeadlines(
        [Description("Filter by case ID")] int? caseId = null,
        [Description("Filter by assigned user ID")] int? userId = null,
        [Description("Filter by urgency: Critical, High, Normal")] string? urgency = null)
        => throw new NotImplementedException("calendar_get_deadlines is not yet implemented.");

    [McpServerTool, Description("Add a hearing, filing deadline, or meeting to the case calendar.")]
    public static string CalendarAddEvent(
        [Description("The case ID to associate the event with")] int caseId,
        [Description("Event type: Hearing, Deadline, Meeting")] string eventType,
        [Description("Title or name of the event")] string title,
        [Description("Date and time of the event (ISO 8601 datetime string)")] string dateTime,
        [Description("JSON array of attendee user IDs")] string? attendees = null,
        [Description("Additional notes for the event")] string? notes = null)
        => throw new NotImplementedException("calendar_add_event is not yet implemented.");

    [McpServerTool, Description("Check for scheduling conflicts for one or more attorneys at a proposed date/time.")]
    public static string CalendarGetConflicts(
        [Description("JSON array of user IDs to check")] string userIds,
        [Description("Proposed event date and time (ISO 8601 datetime string)")] string proposedDateTime,
        [Description("Duration in minutes")] int duration)
        => throw new NotImplementedException("calendar_get_conflicts is not yet implemented.");
}
