# Plan: Dual Transport Implementation (stdio + Streamable HTTP)

**Date:** 2026-02-24
**Related ADR:** [ADR-004](../../decisions/004-dual-transport-web-api-primary.md)
**Research:** [RESEARCH-stdio-vs-http-transport.md](./RESEARCH-stdio-vs-http-transport.md)
**Task:** [1.1.2](./1.1.2-configure-mcp-skeleton.md)

---

## Decision Summary

**Keep dual transport via `appsettings` — it is reasonable.**

The research confirms this is the established pattern in the .NET MCP ecosystem. The C# MCP SDK is designed for transport-agnostic tool code, and the `ModelContextProtocol.AspNetCore` package provides `WithHttpTransport()` for the HTTP path. Claude Desktop still requires stdio, so dropping it is not an option. Defaulting to HTTP and letting stdio clients override via environment variable is the least error-prone approach.

---

## Implementation Tasks

### Phase 1: Migrate Host to ASP.NET Core Web API

| # | Task | Status | Notes |
|---|---|---|---|
| 1.1 | Change `Program.cs` from `Host.CreateApplicationBuilder()` to `WebApplication.CreateBuilder()` | TODO | Minimal diff — the builder API is nearly identical |
| 1.2 | Change `.csproj` SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web` | TODO | Brings in ASP.NET Core framework reference |
| 1.3 | Add NuGet package `ModelContextProtocol.AspNetCore` | TODO | Provides `WithHttpTransport()` |
| 1.4 | Add `Transport:Mode` config key to `appsettings.json` (default: `http`) | TODO | |
| 1.5 | Branch `Program.cs` on `Transport:Mode`: register `WithStdioServerTransport()` or `WithHttpTransport()` + map MCP endpoint | TODO | |
| 1.6 | Fail fast on unknown `Transport:Mode` values | TODO | Throw `InvalidOperationException` at startup |
| 1.7 | Add `/health` endpoint | TODO | Required for Azure Foundry later; trivial with ASP.NET Core |
| 1.8 | Verify existing tools still work in both modes manually | TODO | Quick sanity check before writing automated tests |

### Phase 2: Update Client Configurations

| # | Task | Status | Notes |
|---|---|---|---|
| 2.1 | Update `docs/mcp.json` (the repo-level MCP config) to pass `Transport__Mode=stdio` as env var | TODO | Claude Desktop and Cursor use this |
| 2.2 | Create `.vscode/mcp.json` with stdio config for VS Code / Cursor users | TODO | |
| 2.3 | Document Claude Desktop config in README or `1.1.3-dev-environment-config.md` | TODO | `claude_desktop_config.json` snippet |
| 2.4 | Document HTTP mode usage for browser/curl testing | TODO | `curl -X POST http://localhost:5000/mcp ...` |

### Phase 3: Test Infrastructure Alignment

| # | Task | Status | Notes |
|---|---|---|---|
| 3.1 | Implement `LawCorpWebApplicationFactory` (task 7.1.3) using HTTP mode | TODO | Blocked on Phase 1 completion |
| 3.2 | Implement stdio smoke test (task 7.5.2) — spawn process, send `initialize`, verify response | TODO | Mark `[Trait("Category","E2E")]`, exclude from default CI |
| 3.3 | Verify `WebApplicationFactory` works with MCP SDK's hosted service registration | TODO | Open question from ADR-004 |

---

## `Program.cs` Branching Sketch

```csharp
var builder = WebApplication.CreateBuilder(args);

// Shared registrations (DB, auth, DI) — unchanged regardless of transport
builder.Services.AddLawCorpDatabase(connectionString);
builder.Services.AddScoped<CaseManagementTools>();
// ... other registrations ...

var transportMode = builder.Configuration.GetValue<string>("Transport:Mode") ?? "http";

var mcpBuilder = builder.Services.AddMcpServer().WithToolsFromAssembly();

switch (transportMode.ToLowerInvariant())
{
    case "http":
        mcpBuilder.WithHttpTransport();
        break;
    case "stdio":
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        mcpBuilder.WithStdioServerTransport();
        break;
    default:
        throw new InvalidOperationException(
            $"Unknown Transport:Mode '{transportMode}'. Use 'http' or 'stdio'.");
}

var app = builder.Build();

if (transportMode.Equals("http", StringComparison.OrdinalIgnoreCase))
{
    app.MapMcp();
    app.MapHealthChecks("/health");
}

// Seed mock data (unchanged) ...

await app.RunAsync();
```

---

## Demo Strategy

| Audience | Transport | How |
|---|---|---|
| **Dev using Claude Desktop** | stdio | `mcp.json` / `claude_desktop_config.json` with `Transport__Mode=stdio` env var — automatic, no manual switching |
| **Dev using VS Code / Cursor chat** | stdio (default) or HTTP | `.vscode/mcp.json` with stdio config; or point at running HTTP server for shared scenarios |
| **Stakeholder demo (browser/curl)** | HTTP | `dotnet run` (defaults to HTTP), hit `http://localhost:5000/mcp` with curl or MCP Inspector |
| **CI / Integration tests** | HTTP | `WebApplicationFactory` — no port binding, in-memory |
| **Azure Foundry (production)** | HTTP | Kestrel behind App Service reverse proxy |

The key insight is that **users never need to manually change `appsettings.json`** to switch transports. stdio clients inject the override via their launch config (`env` block), and HTTP is the default. The config switch is not a UI knob — it's infrastructure plumbing.

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Developer forgets `Transport__Mode=stdio` in Claude Desktop config, gets Kestrel startup instead of MCP handshake | Medium | Low (confusing error, easy to fix) | Document clearly in README and `1.1.3-dev-environment-config.md`; add startup log line stating transport mode |
| stdio code path drifts because all tests use HTTP | Medium | Medium | Dedicated stdio smoke test in CI (even if marked slow); run it in nightly builds |
| `ModelContextProtocol.AspNetCore` preview package has breaking API changes before v1.0 | Medium | Medium | Pin version in `.csproj`; review changelog on each SDK update |
| `WebApplicationFactory` doesn't work cleanly with MCP SDK's hosted service | Low | High (blocks all E2E tests) | Validate in Phase 3 task 3.3 before building out full test suite; fallback is `TestServer` with manual startup |

---

## Open Questions

1. **Which SDK version?** Current project uses `ModelContextProtocol` v0.9.0-preview.2. Does `ModelContextProtocol.AspNetCore` exist at that version, or do we need to upgrade to preview.3? Check NuGet before starting Phase 1.

2. **Endpoint path:** The MCP spec uses a single `/mcp` endpoint for Streamable HTTP. Confirm this is what `MapMcp()` registers, or if it's configurable.

3. **Auth in HTTP mode:** ADR-004 mentions `AddJwtBearer` for Entra ID. For the demo phase (`UseAuth=false`), HTTP mode should skip auth middleware entirely. Verify that `AnonymousUserContext` works with the HTTP transport without requiring a bearer token.

---

## Dependencies

```
Phase 1 (this plan)
  ├── Unblocks: 7.1.3 (WebApplicationFactory)
  ├── Unblocks: 7.5.2 (transport config smoke tests)
  ├── Unblocks: 6.4.1 (Azure Foundry deployment)
  └── Depends on: nothing (can start immediately)

Phase 2 (client configs)
  └── Depends on: Phase 1

Phase 3 (test alignment)
  ├── Depends on: Phase 1
  └── Depends on: 7.1.1 (DB provider adapter), 7.1.2 (identity test double)
```
