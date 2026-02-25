using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Commands;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Server.Handlers.Cases;

public class UpdateCaseStatusHandler(LawCorpDbContext db, IUserContext user)
    : IRequestHandler<UpdateCaseStatusCommand, CommandResult>
{
    private static readonly Dictionary<CaseStatus, CaseStatus[]> ValidTransitions = new()
    {
        [CaseStatus.Active] = [CaseStatus.OnHold, CaseStatus.Closed, CaseStatus.Settled],
        [CaseStatus.OnHold] = [CaseStatus.Active]
    };

    public async Task<CommandResult> Handle(UpdateCaseStatusCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<CaseStatus>(request.NewStatus, ignoreCase: true, out var newStatusEnum))
            return new CommandResult(false, "", Error: $"Invalid status '{request.NewStatus}'. Valid values: Active, OnHold, Closed, Settled.");

        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, ct);

        if (c is null)
            return new CommandResult(false, "", Error: $"Case {request.CaseId} not found.");

        if (!ValidTransitions.TryGetValue(c.Status, out var allowed) || !allowed.Contains(newStatusEnum))
        {
            var options = ValidTransitions.TryGetValue(c.Status, out var a)
                ? string.Join(", ", a) : "none";
            return new CommandResult(false, "",
                Error: $"Cannot transition from {c.Status} to {newStatusEnum}. Valid options: {options}.");
        }

        var isLead = c.Assignments.Any(a => a.UserId == user.UserId && a.Role == AssignmentRole.Lead);
        if (!user.IsPartner && !isLead)
            return new CommandResult(false, "", Error: "Only the lead attorney or a partner can update case status.");

        var previousStatus = c.Status;
        c.Status = newStatusEnum;
        if (newStatusEnum is CaseStatus.Closed or CaseStatus.Settled)
            c.CloseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await db.CaseEvents.AddAsync(new CaseEvent
        {
            CaseId = c.Id,
            EventType = CaseEventType.StatusChange,
            Title = $"Status changed to {newStatusEnum}",
            Description = request.Reason ?? $"Case status updated from {previousStatus} to {newStatusEnum}.",
            EventDate = DateTime.UtcNow,
            CreatedById = user.UserId
        }, ct);

        await db.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.UserId.ToString(),
            UserRole = user.Role.ToString(),
            Action = "UpdateStatus",
            EntityType = "Case",
            EntityId = request.CaseId.ToString(),
            Timestamp = DateTime.UtcNow,
            Details = $"Status: {previousStatus} → {newStatusEnum}. Reason: {request.Reason ?? "not provided"}.",
            IpAddress = "mcp"
        }, ct);

        await db.SaveChangesAsync(ct);

        return new CommandResult(true,
            $"Case {request.CaseId} status updated to {newStatusEnum}.",
            new { caseId = request.CaseId, previousStatus = previousStatus.ToString(), newStatus = newStatusEnum.ToString() });
    }
}
