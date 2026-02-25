using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Commands;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Server.Handlers.Cases;

public class AddCaseNoteHandler(LawCorpDbContext db, IUserContext user)
    : IRequestHandler<AddCaseNoteCommand, CommandResult>
{
    public async Task<CommandResult> Handle(AddCaseNoteCommand request, CancellationToken ct)
    {
        if (!user.IsAttorney)
            return new CommandResult(false, "", Error: "You do not have permission to add notes to cases.");

        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, ct);

        if (c is null)
            return new CommandResult(false, "", Error: $"Case {request.CaseId} not found.");

        if (!user.IsPartner && !c.Assignments.Any(a => a.UserId == user.UserId))
            return new CommandResult(false, "", Error: "Access denied: you are not assigned to this case.");

        var noteEvent = new CaseEvent
        {
            CaseId = request.CaseId,
            EventType = CaseEventType.Note,
            Title = request.IsPrivileged ? "[PRIVILEGED] Note" : "Note",
            Description = request.Content,
            EventDate = DateTime.UtcNow,
            CreatedById = user.UserId
        };

        await db.CaseEvents.AddAsync(noteEvent, ct);

        await db.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user.UserId.ToString(),
            UserRole = user.Role.ToString(),
            Action = "AddNote",
            EntityType = "Case",
            EntityId = request.CaseId.ToString(),
            Timestamp = DateTime.UtcNow,
            Details = $"Note added by {user.DisplayName}. Privileged: {request.IsPrivileged}.",
            IpAddress = "mcp"
        }, ct);

        await db.SaveChangesAsync(ct);

        return new CommandResult(true,
            $"Note added to case {request.CaseId}.",
            new
            {
                caseId = request.CaseId,
                eventId = noteEvent.Id,
                isPrivileged = request.IsPrivileged
            });
    }
}
