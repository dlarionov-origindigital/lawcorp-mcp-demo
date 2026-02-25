# Project Rules

> Single source of truth for all AI agent rules. Copied to agent-specific locations by `scripts/sync-agent-rules.ps1`.

---

## Git

**AI assistants do NOT create git commits.** Implement code, verify it, update `.md` status files, and hand off to the developer. The developer decides commit boundaries.

---

## Project Overview

Law-Corp MCP Server — .NET 9 enterprise reference architecture for Model Context Protocol (MCP) with Entra ID On-Behalf-Of (OBO) auth.

| Project | Purpose |
|---|---|
| `LawCorp.Mcp.Core` | Domain models, interfaces, `McpToolName` constants, MediatR contracts (no ASP.NET dependency) |
| `LawCorp.Mcp.Data` | EF Core DbContext + migrations |
| `LawCorp.Mcp.MockData` | Seed data generator |
| `LawCorp.Mcp.Server` | MCP server (HTTP + stdio transport) |
| `LawCorp.Mcp.Server.Handlers` | MediatR CQRS command/query handlers (data source resolution) |
| `LawCorp.Mcp.Web` | Blazor Web App (MCP client, OBO flow) |
| `LawCorp.Mcp.ExternalApi` | Independent downstream API (JWT Bearer, receives OBO tokens) |
| `LawCorp.Mcp.Tests` | Unit tests |
| `LawCorp.Mcp.Tests.E2E` | Playwright E2E |

**Key constants & infrastructure:**

| File | Purpose |
|---|---|
| `src/LawCorp.Mcp.Core/McpToolName.cs` | MCP tool name constants |
| `src/LawCorp.Mcp.Web/WebRoutes.cs` | Web app route constants |
| `src/LawCorp.Mcp.Web/AppConfigKeys.cs` | Configuration key constants |
| `src/LawCorp.Mcp.Web/AppClaimTypes.cs` | Custom claim type constants |
| `src/LawCorp.Mcp.Server/ServerBootstrap.cs` | Transport configuration (HTTP / stdio) |
| `src/LawCorp.Mcp.Server/Auth/ToolPermissionMatrix.cs` | Role-to-tool permission matrix |
| `src/LawCorp.Mcp.Server/Auth/ToolPermissionFilters.cs` | MCP request filter pipeline hooks |

**Key types:** `IFirmIdentityContext`, `IUserContext`, `FirmRole` enum (`Partner, Associate, OfCounsel, Paralegal, LegalAssistant, Intern`), `ServerBootstrap` (`RunHttpAsync` / `RunStdioAsync`), `IDownstreamTokenProvider`

**Auth/transport:** HTTP transport uses Entra ID JWT validated by `UserContextResolutionMiddleware` which resolves `IFirmIdentityContext` in scoped DI. Stdio transport falls back to `AnonymousUserContext`. Transport selected via `Transport` appsettings key (default `http`). Three Entra ID app registrations: MCP Server (JWT Bearer + OBO), Web App (OIDC), External API (JWT Bearer, receives OBO). See `docs/auth-config.md`.

**CQRS dispatch:** MCP tools dispatch via `IMediator` (MediatR). Handlers in `LawCorp.Mcp.Server.Handlers` resolve data sources (local DB, external API via OBO, Graph via OBO). Contracts in `LawCorp.Mcp.Core`. See [ADR-008](proj-mgmt/decisions/008-cqrs-dispatch-pattern.md).

**SDK:** `ModelContextProtocol` 1.0.0-rc.1 + `ModelContextProtocol.AspNetCore` 1.0.0-rc.1

---

## Code Rules

### 1. No magic strings

Enumerable string values are defined once as `const string` fields. Never write raw string literals for these categories.

| Category | Constant class | Example |
|---|---|---|
| MCP tool names | `McpToolName.cs` | `McpToolName.Cases.Search` |
| Web app routes | `WebRoutes.cs` | `WebRoutes.Tools`, `WebRoutes.Auth.SignIn` |
| Config keys | `AppConfigKeys.cs` | `AppConfigKeys.UseAuth` |
| Claim types | `AppClaimTypes.cs` | `AppClaimTypes.Roles` |

```csharp
// CORRECT
[McpServerTool(Name = McpToolName.Cases.Search), Description("...")]
Navigation.NavigateTo(WebRoutes.Auth.SignIn, forceLoad: true);
config.GetValue<bool>(AppConfigKeys.UseAuth);

// WRONG
[McpServerTool(Name = "cases_search"), Description("...")]
Navigation.NavigateTo("MicrosoftIdentity/Account/SignIn", forceLoad: true);
config.GetValue<bool>("UseAuth");
```

**Exception:** Blazor `@page` directives require string literals. Define the constant in `WebRoutes.cs` and set the matching `@page` in the component.

### 2. McpServerTool.Name must always be set

Use `[McpServerTool(Name = McpToolName.X.Y)]`. Do not rely on the SDK's PascalCase-to-snake_case convention.

### 3. Adding new tools

1. Add `const string` to the appropriate nested class in `McpToolName.cs`
2. Set `Name = McpToolName.X.Y` in `[McpServerTool]`
3. Add the tool to `ToolPermissionMatrix` for each role that should have access
4. Define a command/query contract in `LawCorp.Mcp.Core` (e.g., `SearchCasesQuery : IRequest<SearchCasesResult>`)
5. Implement the handler in `LawCorp.Mcp.Server.Handlers`
6. The tool method should only construct the request and call `IMediator.Send()` — it should not inject `DbContext` or `HttpClient` directly

### 4. Adding new web pages

1. Add route constant to `WebRoutes.cs`
2. Reference from `NavMenu.razor`, page links, and `NavigationManager` calls
3. Set matching `@page` directive in the component

### 5. Permission matrix

`ToolPermissionMatrix.cs` maps `FirmRole` to permitted tool names. `ToolPermissionFilters.cs` enforces this on every `tools/list` and `tools/call` request. Anonymous/stdio callers pass through without restriction.

### 6. EF Core

- Never set explicit IDs for operational entities; use `ValueGeneratedNever()` only for reference/lookup data
- Load navigation properties explicitly before accessing them
- Save before querying auto-generated IDs

### 7. Config files

- Use `appsettings.Development.json` (gitignored) + `.example` (tracked) pattern
- Never hardcode secrets or connection strings
- Downstream API configuration uses `DownstreamApis:<ApiName>:BaseUrl` and `DownstreamApis:<ApiName>:Scopes` pattern

### 8. Port allocation (local dev)

| Service | HTTP | HTTPS | Project |
|---|---|---|---|
| MCP Server | 5000 | — | `LawCorp.Mcp.Server` |
| Web App | 5003 | 5001 | `LawCorp.Mcp.Web` |
| External API (DMS) | 5002 | 7002 | `LawCorp.Mcp.ExternalApi` |

### 9. Preserve before removing

When moving data (e.g. extracting config into a gitignored file), write the destination with real values first, then remove from the source. A `.example` with placeholders does not count as preserving.

### 10. Tests

Write regression tests before marking a bug DONE. Test both the fix and that the bug doesn't reoccur.

### 11. Migrations

Create migrations when modifying the schema but don't run them in `Program.cs` unless explicitly needed. Use `EnsureCreatedAsync()` for dev/testing only.

---

## MCP Filter API (SDK 1.0.0-rc.1)

```csharp
builder.Services.AddMcpServer()
    .WithToolsFromAssembly()
    .WithRequestFilters(f => {
        f.AddListToolsFilter(ToolPermissionFilters.ListTools);
        f.AddCallToolFilter(ToolPermissionFilters.CallTool);
    });
```

- `ListToolsResult.Tools` is `IList<Tool>` (sealed — use `.Clear()` + `.Add()`, no `with`)
- `CallToolResult` is sealed with `Content: IList<ContentBlock>` and `IsError: bool?`
- `RequestContext<T>` inherits `MessageContext.Services: IServiceProvider?` (scoped when `ScopeRequests=true`)

---

## Project Management

Work items live in `proj-mgmt/epics/NN-slug/`. Structure: epic (`_epic.md`) > feature (`N.M-slug.md`) > item (`N.M.P-slug.md`). Bugs use the same convention with `Type: Bug`.

**Before implementing:** Read the item file fully. Check `**Status:**`. Review acceptance criteria. Check dependencies and linked ADRs.

**While implementing:** Verify each acceptance criterion. Check for regressions. Document non-obvious choices.

**After implementing:** Check off all criteria. Update `**Status:**` to `DONE`. Update parent feature/epic status tables.

**Status rules:** Never mark `DONE` without verifying all acceptance criteria. Never mark an epic `DONE` if sub-items aren't. Always update parent tables after changing status.

**Systems thinking** (before designing): Consider domain boundaries, authorization impact, data access patterns, protocol surface, dependencies, test surface, and reversibility of decisions. Draft an ADR for costly or non-obvious choices.

---

## Communication

- When SDK/library internals are opaque, ask the user directly. Don't use decompilers or guess.
- Default to updating `RULES.md` when saving a convention. User specifies when a rule should be global.

---

## Rules Architecture

This file is the **single source of truth**. A sync script copies it to agent-specific locations:

| Target | Agent |
|---|---|
| `.cursor/rules/project-rules.mdc` | Cursor (with YAML frontmatter) |
| `.rules/CLAUDE.md` | Claude Code |
| `.github/copilot-instructions.md` | GitHub Copilot |

**Sync:** `powershell scripts/sync-agent-rules.ps1` (will also run as a GitHub Action on commit)

Do **not** edit agent files directly — changes are overwritten on sync.
