using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Core.Queries;
using LawCorp.Mcp.ExternalApi.Auth;
using LawCorp.Mcp.ExternalApi.Data;
using LawCorp.Mcp.ExternalApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.ExternalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController(DmsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? documentType = null,
        [FromQuery] int? caseId = null,
        [FromQuery] string? matterNumber = null,
        [FromQuery] int? authorId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var caller = CallerIdentity.FromClaimsPrincipal(User);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var accessibleWorkspaceIds = await GetAccessibleWorkspaceIds(caller, ct);

        var q = db.Documents
            .Include(d => d.Workspace)
            .Where(d => accessibleWorkspaceIds.Contains(d.WorkspaceId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(d => d.Title.Contains(query) || d.Content.Contains(query));

        if (!string.IsNullOrWhiteSpace(documentType) &&
            Enum.TryParse<DmsDocumentType>(documentType, ignoreCase: true, out var typeEnum))
            q = q.Where(d => d.DocumentType == typeEnum);

        if (!string.IsNullOrWhiteSpace(matterNumber))
            q = q.Where(d => d.Workspace.MatterNumber == matterNumber);
        else if (caseId.HasValue)
            q = q.Where(d => d.WorkspaceId == caseId.Value);

        if (caller.Role is FirmRole.Intern or FirmRole.Paralegal or FirmRole.LegalAssistant)
            q = q.Where(d => !d.IsPrivileged);

        var totalCount = await q.CountAsync(ct);
        var docs = await q
            .OrderByDescending(d => d.ModifiedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DocumentSummary(
                d.Id,
                d.Title,
                d.DocumentType.ToString(),
                d.Status.ToString(),
                d.AuthorName,
                d.WorkspaceId,
                d.Workspace.MatterNumber,
                d.IsPrivileged,
                d.CreatedDate.ToString("yyyy-MM-dd"),
                d.ModifiedDate.ToString("yyyy-MM-dd")))
            .ToListAsync(ct);

        return Ok(new SearchDocumentsResult(docs, page, pageSize, totalCount, (page * pageSize) < totalCount));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var caller = CallerIdentity.FromClaimsPrincipal(User);
        var accessibleWorkspaceIds = await GetAccessibleWorkspaceIds(caller, ct);

        var doc = await db.Documents
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (doc is null) return NotFound(new { error = $"Document {id} not found." });

        if (!accessibleWorkspaceIds.Contains(doc.WorkspaceId))
            return Forbid();

        if (doc.IsPrivileged && caller.Role is FirmRole.Intern or FirmRole.Paralegal or FirmRole.LegalAssistant)
            return Forbid();

        var content = doc.Content;
        if (caller.Role == FirmRole.Intern && doc.IsPrivileged)
            content = "[REDACTED — privileged content not available for your role]";

        return Ok(new GetDocumentResult(new DocumentDetail(
            doc.Id,
            doc.Title,
            doc.DocumentType.ToString(),
            doc.Status.ToString(),
            content,
            doc.AuthorName,
            0,
            doc.WorkspaceId,
            doc.Workspace.MatterNumber,
            doc.IsPrivileged,
            caller.Role == FirmRole.Intern && doc.IsPrivileged,
            doc.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
            doc.ModifiedDate.ToString("yyyy-MM-dd HH:mm"))));
    }

    private async Task<List<int>> GetAccessibleWorkspaceIds(CallerIdentity caller, CancellationToken ct)
    {
        return await db.AccessRules
            .Where(r => r.Role == caller.Role && r.CanRead)
            .Select(r => r.WorkspaceId)
            .ToListAsync(ct);
    }
}
