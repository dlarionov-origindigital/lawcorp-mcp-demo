using System.Security.Claims;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// ASP.NET Core middleware that resolves the authenticated user's identity from the
/// validated JWT and the local database. Queries the unified <c>Users</c> table by
/// <c>EntraObjectId</c>, supporting all personnel types (attorneys, paralegals,
/// legal assistants, interns).
/// </summary>
public sealed class UserContextResolutionMiddleware
{
    internal const string UserContextKey = "LawCorp.ResolvedUserContext";
    internal const string FirmIdentityKey = "LawCorp.ResolvedFirmIdentity";

    private readonly RequestDelegate _next;

    public UserContextResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await ResolveIdentityAsync(context);
        }

        await _next(context);
    }

    private static async Task ResolveIdentityAsync(HttpContext context)
    {
        var oid = context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                  ?? context.User.FindFirstValue("oid");

        if (string.IsNullOrEmpty(oid))
            return;

        var db = context.RequestServices.GetRequiredService<LawCorpDbContext>();

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.CaseAssignments)
            .FirstOrDefaultAsync(u => u.EntraObjectId == oid);

        if (user is null)
            return;

        var displayName = $"{user.FirstName} {user.LastName}";

        context.Items[UserContextKey] = new EntraIdUserContext
        {
            UserId = user.Id,
            DisplayName = displayName,
            Role = user.FirmRole
        };

        context.Items[FirmIdentityKey] = new EntraFirmIdentityContext
        {
            EntraObjectId = oid,
            UserId = user.Id,
            DisplayName = displayName,
            FirmRole = user.FirmRole,
            PracticeGroupId = user.PracticeGroupId,
            AssignedCaseIds = user.CaseAssignments.Select(ca => ca.CaseId).ToList(),
            SupervisorId = user.SupervisorId
        };
    }
}
