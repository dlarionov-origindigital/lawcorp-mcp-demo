using System.Security.Claims;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LawCorp.Mcp.Server.Auth;

/// <summary>
/// ASP.NET Core middleware that resolves the authenticated user's identity from the
/// validated JWT and the local database. Runs after the JWT Bearer middleware and
/// before MCP tool handlers execute.
/// <para>
/// Stores the resolved <see cref="EntraIdUserContext"/> and
/// <see cref="EntraFirmIdentityContext"/> in <c>HttpContext.Items</c> so the scoped
/// DI factories can read them synchronously.
/// </para>
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

        var attorney = await db.Attorneys
            .AsNoTracking()
            .Include(a => a.CaseAssignments)
            .FirstOrDefaultAsync(a => a.EntraObjectId == oid);

        if (attorney is null)
            return;

        var displayName = $"{attorney.FirstName} {attorney.LastName}";

        context.Items[UserContextKey] = new EntraIdUserContext
        {
            UserId = attorney.Id,
            DisplayName = displayName,
            Role = attorney.Role
        };

        context.Items[FirmIdentityKey] = new EntraFirmIdentityContext
        {
            EntraObjectId = oid,
            UserId = attorney.Id,
            DisplayName = displayName,
            AttorneyRole = attorney.Role,
            PracticeGroupId = attorney.PracticeGroupId,
            AssignedCaseIds = attorney.CaseAssignments.Select(ca => ca.CaseId).ToList(),
            AssignedAttorneyId = null
        };
    }
}
