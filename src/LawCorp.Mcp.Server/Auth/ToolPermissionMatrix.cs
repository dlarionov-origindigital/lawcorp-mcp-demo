using LawCorp.Mcp.Core;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// Role-to-tool mapping derived from PRD Section 4.2.
/// All tool names reference <see cref="McpToolName"/> constants — no raw string literals.
/// </summary>
/// <remarks>
/// Row-level scoping (e.g. an Associate seeing only assigned cases) is enforced by
/// EF Core global query filters (1.3.2), not by this matrix. This class only governs
/// which tools appear in <c>tools/list</c> and which calls are permitted.
/// </remarks>
public sealed class ToolPermissionMatrix : IToolPermissionPolicy
{
    private static readonly IReadOnlyDictionary<FirmRole, IReadOnlySet<string>> Matrix =
        new Dictionary<FirmRole, IReadOnlySet<string>>
        {
            [FirmRole.Partner] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cases
                McpToolName.Cases.Search,
                McpToolName.Cases.Get,
                McpToolName.Cases.UpdateStatus,
                McpToolName.Cases.AssignUser,
                McpToolName.Cases.GetTimeline,
                McpToolName.Cases.AddNote,
                // Documents
                McpToolName.Documents.Search,
                McpToolName.Documents.Get,
                McpToolName.Documents.Draft,
                McpToolName.Documents.UpdateStatus,
                McpToolName.Documents.ListByCase,
                // Clients & Contacts
                McpToolName.Clients.Search,
                McpToolName.Clients.Get,
                McpToolName.Clients.ConflictCheck,
                McpToolName.Contacts.Search,
                McpToolName.Contacts.Get,
                // Billing
                McpToolName.Billing.TimeEntriesLog,
                McpToolName.Billing.TimeEntriesSearch,
                McpToolName.Billing.GetSummary,
                McpToolName.Billing.InvoicesSearch,
                McpToolName.Billing.InvoicesGet,
                // Calendar
                McpToolName.Calendar.GetHearings,
                McpToolName.Calendar.GetDeadlines,
                McpToolName.Calendar.AddEvent,
                McpToolName.Calendar.GetConflicts,
                // Research
                McpToolName.Research.SearchPrecedents,
                McpToolName.Research.GetStatute,
                McpToolName.Research.GetMemo,
                McpToolName.Research.CreateMemo,
                McpToolName.Research.SearchMemos,
                // Intake
                McpToolName.Intake.CreateRequest,
                McpToolName.Intake.GetRequest,
                McpToolName.Intake.RunConflictCheck,
                McpToolName.Intake.Approve,
                McpToolName.Intake.GenerateEngagementLetter,
            },

            [FirmRole.Associate] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cases — row-level filter (1.3.2) limits to assigned cases
                McpToolName.Cases.Search,
                McpToolName.Cases.Get,
                McpToolName.Cases.UpdateStatus,
                McpToolName.Cases.GetTimeline,
                McpToolName.Cases.AddNote,
                // Documents
                McpToolName.Documents.Search,
                McpToolName.Documents.Get,
                McpToolName.Documents.Draft,
                McpToolName.Documents.UpdateStatus,
                McpToolName.Documents.ListByCase,
                // Clients & Contacts
                McpToolName.Clients.Search,
                McpToolName.Clients.Get,
                McpToolName.Clients.ConflictCheck,
                McpToolName.Contacts.Search,
                McpToolName.Contacts.Get,
                // Billing — own time entries only (row-level enforced in tool handler)
                McpToolName.Billing.TimeEntriesLog,
                McpToolName.Billing.TimeEntriesSearch,
                // Calendar
                McpToolName.Calendar.GetHearings,
                McpToolName.Calendar.GetDeadlines,
                McpToolName.Calendar.AddEvent,
                McpToolName.Calendar.GetConflicts,
                // Research — full access
                McpToolName.Research.SearchPrecedents,
                McpToolName.Research.GetStatute,
                McpToolName.Research.GetMemo,
                McpToolName.Research.CreateMemo,
                McpToolName.Research.SearchMemos,
                // Intake
                McpToolName.Intake.CreateRequest,
                McpToolName.Intake.GetRequest,
                McpToolName.Intake.RunConflictCheck,
                McpToolName.Intake.GenerateEngagementLetter,
            },

            [FirmRole.OfCounsel] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cases — read-only, own practice group (row-level enforced)
                McpToolName.Cases.Search,
                McpToolName.Cases.Get,
                McpToolName.Cases.GetTimeline,
                McpToolName.Cases.AddNote,
                // Documents — read-only, own practice group
                McpToolName.Documents.Search,
                McpToolName.Documents.Get,
                McpToolName.Documents.ListByCase,
                // Clients & Contacts
                McpToolName.Clients.Search,
                McpToolName.Clients.Get,
                McpToolName.Contacts.Search,
                McpToolName.Contacts.Get,
                // Billing — view own time entries
                McpToolName.Billing.TimeEntriesLog,
                McpToolName.Billing.TimeEntriesSearch,
                // Calendar
                McpToolName.Calendar.GetHearings,
                McpToolName.Calendar.GetDeadlines,
                McpToolName.Calendar.GetConflicts,
                // Research — full access
                McpToolName.Research.SearchPrecedents,
                McpToolName.Research.GetStatute,
                McpToolName.Research.GetMemo,
                McpToolName.Research.CreateMemo,
                McpToolName.Research.SearchMemos,
            },

            [FirmRole.Paralegal] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cases — assigned cases (row-level enforced)
                McpToolName.Cases.Search,
                McpToolName.Cases.Get,
                McpToolName.Cases.GetTimeline,
                McpToolName.Cases.AddNote,
                // Documents — read + draft on assigned cases
                McpToolName.Documents.Search,
                McpToolName.Documents.Get,
                McpToolName.Documents.Draft,
                McpToolName.Documents.ListByCase,
                // Clients & Contacts
                McpToolName.Clients.Search,
                McpToolName.Clients.Get,
                McpToolName.Clients.ConflictCheck,
                McpToolName.Contacts.Search,
                McpToolName.Contacts.Get,
                // Calendar
                McpToolName.Calendar.GetHearings,
                McpToolName.Calendar.GetDeadlines,
                // Research — memos only per PRD (no precedent search or statute)
                McpToolName.Research.GetMemo,
                McpToolName.Research.CreateMemo,
                McpToolName.Research.SearchMemos,
                // Intake
                McpToolName.Intake.CreateRequest,
                McpToolName.Intake.GetRequest,
                McpToolName.Intake.RunConflictCheck,
            },

            [FirmRole.LegalAssistant] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cases — assigned attorney's cases (row-level enforced)
                McpToolName.Cases.Search,
                McpToolName.Cases.Get,
                McpToolName.Cases.GetTimeline,
                // Documents — assigned attorney's cases
                McpToolName.Documents.Search,
                McpToolName.Documents.Get,
                McpToolName.Documents.ListByCase,
                // Contacts
                McpToolName.Contacts.Search,
                McpToolName.Contacts.Get,
                // Calendar — assigned attorney's calendar
                McpToolName.Calendar.GetHearings,
                McpToolName.Calendar.GetDeadlines,
                // Intake
                McpToolName.Intake.CreateRequest,
                McpToolName.Intake.GetRequest,
            },

            [FirmRole.Intern] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // Cases — assigned, read-only (field-level redaction enforced in tool handler)
                McpToolName.Cases.Search,
                McpToolName.Cases.Get,
                // Documents — assigned, read-only (redacted)
                McpToolName.Documents.Search,
                McpToolName.Documents.Get,
                McpToolName.Documents.ListByCase,
                // Calendar — own deadlines only
                McpToolName.Calendar.GetDeadlines,
                // Research — can read and create memos
                McpToolName.Research.GetMemo,
                McpToolName.Research.CreateMemo,
                McpToolName.Research.SearchMemos,
            },
        };

    public bool IsAllowed(string toolName, IFirmIdentityContext identity)
    {
        if (!Matrix.TryGetValue(identity.FirmRole, out var permitted))
            return false;

        return permitted.Contains(toolName);
    }

    public IReadOnlyList<string> GetPermittedTools(IFirmIdentityContext identity)
    {
        if (!Matrix.TryGetValue(identity.FirmRole, out var permitted))
            return [];

        return [.. permitted.Order(StringComparer.OrdinalIgnoreCase)];
    }
}
