using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Core.Queries;
using LawCorp.Mcp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Server.Handlers.Cases;

public class GetCaseTimelineHandler(LawCorpDbContext db, IUserContext user)
    : IRequestHandler<GetCaseTimelineQuery, GetCaseTimelineResult>
{
    public async Task<GetCaseTimelineResult> Handle(GetCaseTimelineQuery request, CancellationToken ct)
    {
        var c = await db.Cases
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, ct);

        if (c is null)
            return new GetCaseTimelineResult(request.CaseId, 0, [],
                Error: $"Case {request.CaseId} not found.");

        if (!user.IsPartner && !c.Assignments.Any(a => a.UserId == user.UserId))
            return new GetCaseTimelineResult(request.CaseId, 0, [],
                Error: "Access denied: you are not assigned to this case.");

        var q = db.CaseEvents.Where(e => e.CaseId == request.CaseId);

        if (!string.IsNullOrWhiteSpace(request.EventType) &&
            Enum.TryParse<CaseEventType>(request.EventType, ignoreCase: true, out var eventTypeEnum))
            q = q.Where(e => e.EventType == eventTypeEnum);

        if (!user.IsAttorney)
            q = q.Where(e => e.EventType != CaseEventType.Note || !e.Title.StartsWith("[PRIVILEGED]"));

        var events = await q.OrderBy(e => e.EventDate).ToListAsync(ct);

        var result = events.Select(e => new TimelineEvent(
            e.Id,
            e.EventType.ToString(),
            e.Title,
            e.Description,
            e.EventDate.ToString("yyyy-MM-dd HH:mm") + " UTC",
            e.CreatedById
        )).ToList();

        return new GetCaseTimelineResult(request.CaseId, result.Count, result);
    }
}
