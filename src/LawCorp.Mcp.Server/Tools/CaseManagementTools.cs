using LawCorp.Mcp.Core;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LawCorp.Mcp.Server.Tools;

[McpServerToolType]
public class CaseManagementTools(LawCorpDbContext db, IUserContext user)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── cases_search ─────────────────────────────────────────────────────────

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
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var q = db.Cases
            .Include(c => c.PracticeGroup)
            .Include(c => c.Client)
            .Include(c => c.Assignments).ThenInclude(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(c =>
                c.Title.Contains(query) ||
                c.Description.Contains(query) ||
                c.CaseNumber.Contains(query));

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<CaseStatus>(status, ignoreCase: true, out var statusEnum))
            q = q.Where(c => c.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(practiceGroup))
            q = q.Where(c => c.PracticeGroup.Name.Contains(practiceGroup));

        if (assignedTo.HasValue)
            q = q.Where(c => c.Assignments.Any(a => a.UserId == assignedTo.Value));

        if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from))
            q = q.Where(c => c.OpenDate >= from);

        if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to))
            q = q.Where(c => c.OpenDate <= to);

        // Row-level access: Partners see all cases; Associates/OfCounsel see only assigned cases
        // TODO This is access control and could be relegated to a centralized authorization service in the future
        // but we'll enforce it here for simplicity for now
        if (!user.IsPartner)
            q = q.Where(c => c.Assignments.Any(a => a.UserId == user.UserId));

        var totalCount = await q.CountAsync(ct);
        var cases = await q
            .OrderByDescending(c => c.OpenDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var results = cases.Select(c => new
        {
            id = c.Id,
            caseNumber = c.CaseNumber,
            title = c.Title,
            status = c.Status.ToString(),
            practiceGroup = c.PracticeGroup.Name,
            clientName = c.Client.Name,
            openDate = c.OpenDate.ToString("yyyy-MM-dd"),
            estimatedValue = c.EstimatedValue,
            leadAttorney = c.Assignments
                .Where(a => a.Role == AssignmentRole.Lead)
                .Select(a => $"{a.User.FirstName} {a.User.LastName}")
                .FirstOrDefault()
        });

        return JsonSerializer.Serialize(new
        {
            results,
            page,
            pageSize,
            totalCount,
            hasMore = (page * pageSize) < totalCount
        }, JsonOpts);
    }

    // ── cases_get ─────────────────────────────────────────────────────────────

    [McpServerTool(Name = McpToolName.Cases.Get), Description(
        "Retrieve full case details by case ID, including assigned team, parties, court, and key dates.")]
    public async Task<string> CasesGet(
        [Description("The unique case ID")] int caseId,
        CancellationToken ct = default)
    {
        var c = await db.Cases
            .Include(c => c.PracticeGroup)
            .Include(c => c.Client)
            .Include(c => c.Court)
            .Include(c => c.Assignments).ThenInclude(a => a.User)
            .Include(c => c.Parties)
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (c is null)
            return Error($"Case {caseId} not found.");

        if (!user.IsPartner && !c.Assignments.Any(a => a.UserId == user.UserId))
            return Error("Access denied: you are not assigned to this case.");

        return JsonSerializer.Serialize(new
        {
            id = c.Id,
            caseNumber = c.CaseNumber,
            title = c.Title,
            description = c.Description,
            status = c.Status.ToString(),
            practiceGroup = c.PracticeGroup.Name,
            client = new
            {
                id = c.Client.Id,
                name = c.Client.Name,
                type = c.Client.Type.ToString(),
                industry = c.Client.Industry
            },
            court = c.Court is null ? null : new
            {
                name = c.Court.Name,
                jurisdiction = c.Court.Jurisdiction
            },
            openDate = c.OpenDate.ToString("yyyy-MM-dd"),
            closeDate = c.CloseDate?.ToString("yyyy-MM-dd"),
            estimatedValue = c.EstimatedValue,
            team = c.Assignments.Select(a => new
            {
                name = $"{a.User.FirstName} {a.User.LastName}",
                email = a.User.Email,
                role = a.Role.ToString(),
                since = a.AssignedDate.ToString("yyyy-MM-dd")
            }),
            parties = c.Parties.Select(p => new
            {
                name = p.Name,
                role = p.PartyType.ToString(),
                representation = p.Representation
            })
        }, JsonOpts);
    }

    // ── cases_update_status ───────────────────────────────────────────────────

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
        if (!Enum.TryParse<CaseStatus>(newStatus, ignoreCase: true, out var newStatusEnum))
            return Error($"Invalid status '{newStatus}'. Valid values: Active, OnHold, Closed, Settled.");

        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (c is null)
            return Error($"Case {caseId} not found.");

        // Validate transition
        var validTransitions = new Dictionary<CaseStatus, CaseStatus[]>
        {
            [CaseStatus.Active]  = [CaseStatus.OnHold, CaseStatus.Closed, CaseStatus.Settled],
            [CaseStatus.OnHold]  = [CaseStatus.Active]
        };

        if (!validTransitions.TryGetValue(c.Status, out var allowed) || !allowed.Contains(newStatusEnum))
        {
            var options = validTransitions.TryGetValue(c.Status, out var a)
                ? string.Join(", ", a)
                : "none";
            return Error($"Cannot transition from {c.Status} to {newStatusEnum}. Valid options from {c.Status}: {options}.");
        }

        // Auth: lead attorney or partner
        var isLead = c.Assignments.Any(a => a.UserId == user.UserId && a.Role == AssignmentRole.Lead);
        if (!user.IsPartner && !isLead)
            return Error("Only the lead attorney or a partner can update case status.");

        var previousStatus = c.Status;
        c.Status = newStatusEnum;
        if (newStatusEnum is CaseStatus.Closed or CaseStatus.Settled)
            c.CloseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await db.CaseEvents.AddAsync(new CaseEvent
        {
            CaseId = c.Id,
            EventType = CaseEventType.StatusChange,
            Title = $"Status changed to {newStatusEnum}",
            Description = reason ?? $"Case status updated from {previousStatus} to {newStatusEnum}.",
            EventDate = DateTime.UtcNow,
            CreatedById = user.UserId
        }, ct);

        await WriteAuditLog("UpdateStatus", "Case", caseId,
            $"Status: {previousStatus} → {newStatusEnum}. Reason: {reason ?? "not provided"}.");

        await db.SaveChangesAsync(ct);

        return JsonSerializer.Serialize(new
        {
            success = true,
            caseId,
            previousStatus = previousStatus.ToString(),
            newStatus = newStatusEnum.ToString(),
            message = $"Case {caseId} status updated to {newStatusEnum}."
        }, JsonOpts);
    }

    // ── cases_assign_attorney ─────────────────────────────────────────────────

    [McpServerTool(Name = McpToolName.Cases.AssignUser), Description(
        "Assign or reassign a user to a case with a specified role. Partner-only action.")]
    public async Task<string> CasesAssignUser(
        [Description("The unique case ID")] int caseId,
        [Description("The user ID to assign")] int userId,
        [Description("Role on the case: Lead, Supporting, or Reviewer")] string role,
        CancellationToken ct = default)
    {
        if (!user.IsPartner)
            return Error("Only partners can assign users to cases.");

        if (!Enum.TryParse<AssignmentRole>(role, ignoreCase: true, out var roleEnum))
            return Error($"Invalid role '{role}'. Valid values: Lead, Supporting, Reviewer.");

        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (c is null)
            return Error($"Case {caseId} not found.");

        var assignee = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (assignee is null)
            return Error($"User {userId} not found.");

        if (!assignee.IsActive)
            return Error($"{assignee.FirstName} {assignee.LastName} is inactive and cannot be assigned to cases.");

        var existing = c.Assignments.FirstOrDefault(a => a.UserId == userId);
        string action;
        if (existing is not null)
        {
            existing.Role = roleEnum;
            action = "reassigned";
        }
        else
        {
            await db.CaseAssignments.AddAsync(new CaseAssignment
            {
                CaseId = caseId,
                UserId = userId,
                Role = roleEnum,
                AssignedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            }, ct);
            action = "assigned";
        }

        await db.CaseEvents.AddAsync(new CaseEvent
        {
            CaseId = caseId,
            EventType = CaseEventType.Assignment,
            Title = $"User {action}: {assignee.FirstName} {assignee.LastName}",
            Description = $"{assignee.FirstName} {assignee.LastName} {action} as {roleEnum}.",
            EventDate = DateTime.UtcNow,
            CreatedById = user.UserId
        }, ct);

        await WriteAuditLog("AssignUser", "Case", caseId,
            $"{assignee.FirstName} {assignee.LastName} (ID {userId}) {action} as {roleEnum}.");

        await db.SaveChangesAsync(ct);

        return JsonSerializer.Serialize(new
        {
            success = true,
            caseId,
            userId,
            assignee = $"{assignee.FirstName} {assignee.LastName}",
            role = roleEnum.ToString(),
            action,
            message = $"{assignee.FirstName} {assignee.LastName} has been {action} as {roleEnum} on case {caseId}."
        }, JsonOpts);
    }

    // ── cases_get_timeline ────────────────────────────────────────────────────

    [McpServerTool(Name = McpToolName.Cases.GetTimeline), Description(
        "Retrieve a chronological timeline of all events for a case. " +
        "Optionally filter by event type. Privileged notes are hidden from non-attorneys.")]
    public async Task<string> CasesGetTimeline(
        [Description("The unique case ID")] int caseId,
        [Description("Filter by event type: StatusChange, Assignment, Note, Filing, Hearing, Deadline, DocumentAdded, Other")] string? eventType = null,
        CancellationToken ct = default)
    {
        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (c is null)
            return Error($"Case {caseId} not found.");

        if (!user.IsPartner && !c.Assignments.Any(a => a.UserId == user.UserId))
            return Error("Access denied: you are not assigned to this case.");

        var q = db.CaseEvents.Where(e => e.CaseId == caseId);

        if (!string.IsNullOrWhiteSpace(eventType) &&
            Enum.TryParse<CaseEventType>(eventType, ignoreCase: true, out var eventTypeEnum))
            q = q.Where(e => e.EventType == eventTypeEnum);

        // Privileged notes (title starts with "[PRIVILEGED]") are hidden from non-attorneys
        if (!user.IsAttorney)
            q = q.Where(e => e.EventType != CaseEventType.Note || !e.Title.StartsWith("[PRIVILEGED]"));

        var events = await q.OrderBy(e => e.EventDate).ToListAsync(ct);

        return JsonSerializer.Serialize(new
        {
            caseId,
            eventCount = events.Count,
            events = events.Select(e => new
            {
                id = e.Id,
                type = e.EventType.ToString(),
                title = e.Title,
                description = e.Description,
                date = e.EventDate.ToString("yyyy-MM-dd HH:mm") + " UTC",
                createdById = e.CreatedById
            })
        }, JsonOpts);
    }

    // ── cases_add_note ────────────────────────────────────────────────────────

    [McpServerTool(Name = McpToolName.Cases.AddNote), Description(
        "Add a note or comment to a case record. " +
        "Privileged notes are visible only to attorneys (not paralegals or interns).")]
    public async Task<string> CasesAddNote(
        [Description("The unique case ID")] int caseId,
        [Description("The note content")] string content,
        [Description("Mark as attorney-client privileged — hides note from paralegals and interns")] bool isPrivileged = false,
        CancellationToken ct = default)
    {
        // Only attorneys can add notes (paralegals may add non-privileged notes once their role is in IUserContext)
        if (!user.IsAttorney)
            return Error("You do not have permission to add notes to cases.");

        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == caseId, ct);

        if (c is null)
            return Error($"Case {caseId} not found.");

        if (!user.IsPartner && !c.Assignments.Any(a => a.UserId == user.UserId))
            return Error("Access denied: you are not assigned to this case.");

        var noteEvent = new CaseEvent
        {
            CaseId = caseId,
            EventType = CaseEventType.Note,
            Title = isPrivileged ? "[PRIVILEGED] Note" : "Note",
            Description = content,
            EventDate = DateTime.UtcNow,
            CreatedById = user.UserId
        };

        await db.CaseEvents.AddAsync(noteEvent, ct);

        await WriteAuditLog("AddNote", "Case", caseId,
            $"Note added by {user.DisplayName}. Privileged: {isPrivileged}.");

        await db.SaveChangesAsync(ct);

        return JsonSerializer.Serialize(new
        {
            success = true,
            caseId,
            eventId = noteEvent.Id,
            isPrivileged,
            message = $"Note added to case {caseId}."
        }, JsonOpts);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static string Error(string message) =>
        JsonSerializer.Serialize(new { error = message }, JsonOpts);

    private async Task WriteAuditLog(string action, string entityType, int entityId, string details)
    {
        await db.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.UserId.ToString(),
            UserRole = user.Role.ToString(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Timestamp = DateTime.UtcNow,
            Details = details,
            IpAddress = "stdio"
        });
    }
}
