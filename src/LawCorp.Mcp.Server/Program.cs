using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Data;
using LawCorp.Mcp.MockData;
using LawCorp.Mcp.MockData.Profiles;
using LawCorp.Mcp.Server.Auth;
using LawCorp.Mcp.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Route all logs to stderr so they don't corrupt the stdio MCP stream
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// ── Database ─────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("LawCorpDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "LawCorpDb connection string is missing. " +
        "Copy appsettings.Development.json.example → appsettings.Development.json and fill in your local SQL Express connection string.");

builder.Services.AddLawCorpDatabase(connectionString);

// ── Auth context ──────────────────────────────────────────────────────────────
// UseAuth=false  → AnonymousUserContext (demo/dev, full Partner access, no Entra ID needed)
// UseAuth=true   → not yet implemented; reserved for Epic 1.2 (Entra ID OBO flow)
var useAuth = builder.Configuration.GetValue<bool>("UseAuth");
if (useAuth)
    throw new NotImplementedException(
        "Entra ID authentication is not yet implemented. Set UseAuth=false in appsettings for demo mode.");
else
    builder.Services.AddSingleton<IUserContext, AnonymousUserContext>();

// ── Tool types (non-static; resolved from DI per invocation) ─────────────────
builder.Services.AddScoped<CaseManagementTools>();

// ── MCP server ────────────────────────────────────────────────────────────────
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// ── Startup: ensure DB schema exists and seed demo data ──────────────────────
var seedMockData = builder.Configuration.GetValue<bool>("SeedMockData");
if (seedMockData)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LawCorpDbContext>();
    // Ensure a clean database by deleting and recreating it
    // This forces the schema to be rebuilt with the latest entity configurations
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    var seeder = new MockDataSeeder(db, new SmallFirmProfile());
    await seeder.SeedAsync();
}

await app.RunAsync();
