using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Core.Queries;
using LawCorp.Mcp.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.Server.Handlers.Cases;

public class SearchCasesHandler(LawCorpDbContext db, IUserContext user)
    : IRequestHandler<SearchCasesQuery, SearchCasesResult>
{
    public async Task<SearchCasesResult> Handle(SearchCasesQuery request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(request.Page, 1);

        var q = db.Cases
            .Include(c => c.PracticeGroup)
            .Include(c => c.Client)
            .Include(c => c.Assignments).ThenInclude(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
            q = q.Where(c =>
                c.Title.Contains(request.Query) ||
                c.Description.Contains(request.Query) ||
                c.CaseNumber.Contains(request.Query));

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<CaseStatus>(request.Status, ignoreCase: true, out var statusEnum))
            q = q.Where(c => c.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(request.PracticeGroup))
            q = q.Where(c => c.PracticeGroup.Name.Contains(request.PracticeGroup));

        if (request.AssignedTo.HasValue)
            q = q.Where(c => c.Assignments.Any(a => a.UserId == request.AssignedTo.Value));

        if (!string.IsNullOrWhiteSpace(request.DateFrom) && DateOnly.TryParse(request.DateFrom, out var from))
            q = q.Where(c => c.OpenDate >= from);

        if (!string.IsNullOrWhiteSpace(request.DateTo) && DateOnly.TryParse(request.DateTo, out var to))
            q = q.Where(c => c.OpenDate <= to);

        if (!user.IsPartner)
            q = q.Where(c => c.Assignments.Any(a => a.UserId == user.UserId));

        var totalCount = await q.CountAsync(ct);
        var cases = await q
            .OrderByDescending(c => c.OpenDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var results = cases.Select(c => new CaseSummary(
            c.Id,
            c.CaseNumber,
            c.Title,
            c.Status.ToString(),
            c.PracticeGroup.Name,
            c.Client.Name,
            c.OpenDate.ToString("yyyy-MM-dd"),
            c.EstimatedValue,
            c.Assignments
                .Where(a => a.Role == AssignmentRole.Lead)
                .Select(a => $"{a.User.FirstName} {a.User.LastName}")
                .FirstOrDefault()
        )).ToList();

        return new SearchCasesResult(results, page, pageSize, totalCount, (page * pageSize) < totalCount);
    }
}
