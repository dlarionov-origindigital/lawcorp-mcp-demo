using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Core.Queries;
using LawCorp.Mcp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Server.Handlers.Cases;

public class GetCaseByIdHandler(LawCorpDbContext db, IUserContext user)
    : IRequestHandler<GetCaseByIdQuery, GetCaseResult>
{
    public async Task<GetCaseResult> Handle(GetCaseByIdQuery request, CancellationToken ct)
    {
        var c = await db.Cases
            .Include(c => c.PracticeGroup)
            .Include(c => c.Client)
            .Include(c => c.Court)
            .Include(c => c.Assignments).ThenInclude(a => a.User)
            .Include(c => c.Parties)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, ct);

        if (c is null)
            return new GetCaseResult(null, $"Case {request.CaseId} not found.");

        if (!user.IsPartner && !c.Assignments.Any(a => a.UserId == user.UserId))
            return new GetCaseResult(null, "Access denied: you are not assigned to this case.");

        var detail = new CaseDetail(
            c.Id,
            c.CaseNumber,
            c.Title,
            c.Description,
            c.Status.ToString(),
            c.PracticeGroup.Name,
            new CaseClientInfo(c.Client.Id, c.Client.Name, c.Client.Type.ToString(), c.Client.Industry),
            c.Court is null ? null : new CaseCourtInfo(c.Court.Name, c.Court.Jurisdiction),
            c.OpenDate.ToString("yyyy-MM-dd"),
            c.CloseDate?.ToString("yyyy-MM-dd"),
            c.EstimatedValue,
            c.Assignments.Select(a => new CaseTeamMember(
                $"{a.User.FirstName} {a.User.LastName}",
                a.User.Email,
                a.Role.ToString(),
                a.AssignedDate.ToString("yyyy-MM-dd")
            )).ToList(),
            c.Parties.Select(p => new CasePartyInfo(
                p.Name,
                p.PartyType.ToString(),
                p.Representation
            )).ToList()
        );

        return new GetCaseResult(detail);
    }
}
