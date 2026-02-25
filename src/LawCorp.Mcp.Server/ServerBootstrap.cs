using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Data;
using LawCorp.Mcp.MockData;
using LawCorp.Mcp.MockData.Personas;
using LawCorp.Mcp.MockData.Profiles;
using LawCorp.Mcp.Server.Auth;
using LawCorp.Mcp.Server.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.Server;
using Scalar.AspNetCore;

namespace LawCorp.Mcp.Server;

/// <summary>
/// Configures and runs the MCP server in one of two transport modes:
/// <list type="bullet">
///   <item><b>stdio</b> — Generic Host, communicates over stdin/stdout.
///     Used by MCP clients (Claude Desktop, VS Code) that launch the server as a subprocess.</item>
///   <item><b>http</b> — ASP.NET Core WebApplication, Streamable HTTP + SSE.
///     Supports auth middleware, intended for deployed/authenticated scenarios and MCP Inspector testing.</item>
/// </list>
/// Transport is selected by the <c>Transport</c> appsettings key (default: <c>stdio</c>).
/// Auth (<c>UseAuth</c>) is an independent axis — on HTTP it wires Entra ID middleware;
/// on stdio it is not yet supported and falls back to <see cref="AnonymousUserContext"/>.
/// </summary>
public static class ServerBootstrap
{
    /// <summary>
    /// Reads <c>Transport</c> from configuration before the host is built,
    /// since the host type itself depends on this value.
    /// </summary>
    public static string ResolveTransport(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        return config.GetValue<string>("Transport") ?? "stdio";
    }

    // ── HTTP host ────────────────────────────────────────────────────────────

    public static async Task RunHttpAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        AddPersonaSeedConfig(builder.Configuration);

        var connectionString = GetRequiredConnectionString(builder.Configuration);
        builder.Services.AddLawCorpDatabase(connectionString);

        ConfigureAuth(builder.Services, builder.Configuration);
        RegisterSharedServices(builder.Services, builder.Configuration);

        builder.Services.AddHealthChecks();

        if (!builder.Environment.IsProduction())
            builder.Services.AddOpenApi();

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly()
            .WithRequestFilters(f =>
            {
                f.AddListToolsFilter(ToolPermissionFilters.ListTools);
                f.AddCallToolFilter(ToolPermissionFilters.CallTool);
            });

        var app = builder.Build();

        if (builder.Configuration.GetValue<bool>("UseAuth"))
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<UserContextResolutionMiddleware>();
        }

        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponse
        }).AllowAnonymous();

        if (!app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapMcp("/mcp");

        await SeedIfConfiguredAsync(app.Services, builder.Configuration);
        await app.RunAsync();
    }

    // ── Stdio host ───────────────────────────────────────────────────────────

    public static async Task RunStdioAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        AddPersonaSeedConfig(builder.Configuration);
        ConfigureStdioLogging(builder.Logging);

        var connectionString = GetRequiredConnectionString(builder.Configuration);
        builder.Services.AddLawCorpDatabase(connectionString);

        var useAuth = builder.Configuration.GetValue<bool>("UseAuth");
        if (useAuth)
        {
            Console.Error.WriteLine(
                "WARNING: UseAuth=true is not supported on stdio transport. " +
                "Auth requires HTTP transport (set Transport=http in appsettings). " +
                "Falling back to AnonymousUserContext.");
        }

        builder.Services.AddSingleton<IUserContext, AnonymousUserContext>();
        RegisterSharedServices(builder.Services, builder.Configuration);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly()
            .WithRequestFilters(f =>
            {
                f.AddListToolsFilter(ToolPermissionFilters.ListTools);
                f.AddCallToolFilter(ToolPermissionFilters.CallTool);
            });

        var app = builder.Build();

        await SeedIfConfiguredAsync(app.Services, builder.Configuration);
        await app.RunAsync();
    }

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static void AddPersonaSeedConfig(ConfigurationManager configuration)
    {
        configuration.AddJsonFile("persona-seed.json", optional: true, reloadOnChange: false);
    }

    private static void ConfigureStdioLogging(ILoggingBuilder logging)
    {
        logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
    }

    private static string GetRequiredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LawCorpDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "LawCorpDb connection string is missing. " +
                "Copy appsettings.Development.json.example → appsettings.Development.json " +
                "and fill in your local SQL Express connection string.");
        return connectionString;
    }

    private static void ConfigureAuth(IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("UseAuth"))
            services.AddEntraIdAuthentication(configuration);
        else
            services.AddSingleton<IUserContext, AnonymousUserContext>();
    }

    private static void RegisterSharedServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IToolPermissionPolicy, ToolPermissionMatrix>();
        services.AddScoped<CaseManagementTools>();
        services.AddScoped<DocumentTools>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Handlers.Cases.SearchCasesHandler>();
        });

        var externalApiBaseUrl = configuration["DownstreamApis:ExternalApi:BaseUrl"] ?? "http://localhost:5002";
        services.AddHttpClient("ExternalApi", client =>
        {
            client.BaseAddress = new Uri(externalApiBaseUrl);
        });

        if (!configuration.GetValue<bool>("UseAuth"))
        {
            services.AddSingleton<IDownstreamTokenProvider, NoOpTokenProvider>();
        }
    }

    private static async Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            service = "LawCorp MCP Server",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsJsonAsync(response);
    }

    private static async Task SeedIfConfiguredAsync(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("SeedMockData"))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LawCorpDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var personaSeedConfig = configuration.GetSection("PersonaSeed").Get<PersonaSeedConfig>();
        var seeder = new MockDataSeeder(db, new SmallFirmProfile(), personaSeedConfig);
        await seeder.SeedAsync();
    }
}
