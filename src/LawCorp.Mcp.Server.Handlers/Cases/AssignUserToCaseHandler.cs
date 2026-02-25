using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Commands;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Server.Handlers.Cases;

public class AssignUserToCaseHandler(LawCorpDbContext db, IUserContext user)
    : IRequestHandler<AssignUserToCaseCommand, CommandResult>
{
    public async Task<CommandResult> Handle(AssignUserToCaseCommand request, CancellationToken ct)
    {
        if (!user.IsPartner)
            return new CommandResult(false, "", Error: "Only partners can assign users to cases.");

        if (!Enum.TryParse<AssignmentRole>(request.Role, ignoreCase: true, out var roleEnum))
            return new CommandResult(false, "", Error: $"Invalid role '{request.Role}'. Valid values: Lead, Supporting, Reviewer.");

        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, ct);

        if (c is null)
            return new CommandResult(false, "", Error: $"Case {request.CaseId} not found.");

        var assignee = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (assignee is null)
            return new CommandResult(false, "", Error: $"User {request.UserId} not found.");

        if (!assignee.IsActive)
            return new CommandResult(false, "",
                Error: $"{assignee.FirstName} {assignee.LastName} is inactive and cannot be assigned to cases.");

        var existing = c.Assignments.FirstOrDefault(a => a.UserId == request.UserId);
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
                CaseId = request.CaseId,
                UserId = request.UserId,
                Role = roleEnum,
                AssignedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            }, ct);
            action = "assigned";
        }

        await db.CaseEvents.AddAsync(new CaseEvent
        {
            CaseId = request.CaseId,
            EventType = CaseEventType.Assignment,
            Title = $"User {action}: {assignee.FirstName} {assignee.LastName}",
            Description = $"{assignee.FirstName} {assignee.LastName} {action} as {roleEnum}.",
            EventDate = DateTime.UtcNow,
            CreatedById = user.UserId
        }, ct);

        await db.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.UserId.ToString(),
            UserRole = user.Role.ToString(),
            Action = "AssignUser",
            EntityType = "Case",
            EntityId = request.CaseId.ToString(),
            Timestamp = DateTime.UtcNow,
            Details = $"{assignee.FirstName} {assignee.LastName} (ID {request.UserId}) {action} as {roleEnum}.",
            IpAddress = "mcp"
        }, ct);

        await db.SaveChangesAsync(ct);

        return new CommandResult(true,
            $"{assignee.FirstName} {assignee.LastName} has been {action} as {roleEnum} on case {request.CaseId}.",
            new
            {
                caseId = request.CaseId,
                userId = request.UserId,
                assignee = $"{assignee.FirstName} {assignee.LastName}",
                role = roleEnum.ToString(),
                action
            });
    }
}
