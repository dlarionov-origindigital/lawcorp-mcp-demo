using LawCorp.Mcp.Web;
using LawCorp.Mcp.Web.Components;
using LawCorp.Mcp.Web.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication (Entra ID OIDC) ──────────────────────────────────────────
var useAuth = builder.Configuration.GetValue<bool>(AppConfigKeys.UseAuth);

if (useAuth)
{
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(AppConfigKeys.AzureAd.Section))
        .EnableTokenAcquisitionToCallDownstreamApi(
            builder.Configuration.GetSection(AppConfigKeys.McpServer.Scopes).Get<string[]>() ?? [])
        .AddInMemoryTokenCaches();

    builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// ── UI ──────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

// ── MCP Client ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IMcpClientService, McpClientService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(WebRoutes.Error, createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

if (useAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
