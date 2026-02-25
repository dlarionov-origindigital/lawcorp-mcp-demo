# Tool Permission System

This document describes how role-based tool access control (RBAC) is implemented for the MCP server. It covers architecture, the full permission matrix, filter pipeline behavior, and how to extend the system.

**Related:**
- [Story 1.3.6: Tool permission matrix](../proj-mgmt/epics/01-foundation/1.3.6-tool-permission-matrix.md) — user story and acceptance criteria
- [Feature 1.3: Authorization](../proj-mgmt/epics/01-foundation/1.3-authorization.md) — broader auth feature context
- [`docs/auth-config.md`](./auth-config.md) — Entra ID setup and OBO token exchange

---

## Overview

When an authenticated user calls the MCP server, two things happen automatically at the protocol level — before any tool handler runs:

1. **`tools/list` is filtered** — the response only contains tools the caller's role is permitted to use.
2. **`tools/call` is guarded** — if the caller requests a tool outside their permitted set, the server returns a structured `isError: true` response immediately. The tool handler is never invoked.

Anonymous callers (stdio transport, or HTTP without `UseAuth=true`) bypass both filters entirely and see all tools.

---

## Architecture

Three components work together, all registered in `ServerBootstrap.RegisterToolTypes()`:

```
McpToolName (Core)
    ↓ referenced by
ToolPermissionMatrix (Server/Auth)           ← implements IToolPermissionPolicy
    ↓ queried by
ToolPermissionFilters (Server/Auth)          ← MCP request filter delegates
    ↓ wired via
ServerBootstrap → .WithRequestFilters(...)   ← both HTTP and stdio paths
```

### `McpToolName` — `src/LawCorp.Mcp.Core/McpToolName.cs`

Central registry of all MCP tool name constants. Every tool name string exists exactly once here, as a `const string` in a nested static class grouped by domain:

```csharp
public static class McpToolName
{
    public static class Cases
    {
        public const string Search = "cases_search";
        public const string Get    = "cases_get";
        // ...
    }
    public static class Billing { ... }
    // ...
}
```

These constants are used in two places:
- `[McpServerTool(Name = McpToolName.Cases.Search)]` on each tool method
- `ToolPermissionMatrix` to define which tools each role can access

Never write raw tool name string literals — always reference `McpToolName.X.Y`.

### `IToolPermissionPolicy` — `src/LawCorp.Mcp.Core/Auth/IToolPermissionPolicy.cs`

```csharp
public interface IToolPermissionPolicy
{
    bool IsAllowed(string toolName, IFirmIdentityContext identity);
    IReadOnlyList<string> GetPermittedTools(IFirmIdentityContext identity);
}
```

Registered as a singleton: `services.AddSingleton<IToolPermissionPolicy, ToolPermissionMatrix>()`.

### `ToolPermissionMatrix` — `src/LawCorp.Mcp.Server/Auth/ToolPermissionMatrix.cs`

Implements `IToolPermissionPolicy` with a static `Dictionary<FirmRole, IReadOnlySet<string>>`. Each role maps to the set of tool names it may call. The sets use `StringComparer.OrdinalIgnoreCase` so casing differences never cause silent denials.

`IsAllowed()` does a direct set lookup. `GetPermittedTools()` returns the permitted set sorted alphabetically.

> **Note on scoping:** This matrix governs *which tools are accessible* — it answers "can this role call this tool at all?" Row-level scoping (e.g. an Associate only seeing their assigned cases) is enforced separately by EF Core global query filters (story 1.3.2), not by this matrix.

### `ToolPermissionFilters` — `src/LawCorp.Mcp.Server/Auth/ToolPermissionFilters.cs`

Two static `McpRequestFilter` delegates wired into the SDK's request pipeline:

**`ListTools` filter** — runs *after* the default handler returns the full tool list, then removes any tool not in the caller's permitted set. The mutation is done in-place (`.Clear()` + re-add) because `ListToolsResult` is a sealed class.

**`CallTool` filter** — runs *before* the tool handler. If the requested tool is not permitted, it returns:
```json
{
  "content": [{ "type": "text", "text": "Access denied: the 'Intern' role is not permitted to call 'billing_invoices_get'." }],
  "isError": true
}
```

Both filters check `IFirmIdentityContext` via `context.Services.GetService<IFirmIdentityContext>()`. If the result is `null` (anonymous caller), the filter returns unchanged — full access is preserved for stdio/dev use.

---

## Permission Matrix

Roles are defined in `FirmRole` (Core). The full tool roster spans 8 domains, 35 tools. A checkmark means the role may call the tool; the tool will also appear in their `tools/list` response.

> **Legend:** ✓ = permitted  ·  — = denied  ·  *row-level* = permitted but further scoped by query filters

### Cases

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `cases_search` | ✓ | ✓ *row-level* | ✓ *row-level* | ✓ *row-level* | ✓ *row-level* | ✓ *row-level* |
| `cases_get` | ✓ | ✓ *row-level* | ✓ *row-level* | ✓ *row-level* | ✓ *row-level* | ✓ *row-level* |
| `cases_update_status` | ✓ | ✓ | — | — | — | — |
| `cases_assign_user` | ✓ | — | — | — | — | — |
| `cases_get_timeline` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `cases_add_note` | ✓ | ✓ | ✓ | ✓ | — | — |

### Documents

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `documents_search` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `documents_get` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `documents_draft` | ✓ | ✓ | — | ✓ | — | — |
| `documents_update_status` | ✓ | ✓ | — | — | — | — |
| `documents_list_by_case` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

### Clients

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `clients_search` | ✓ | ✓ | ✓ | ✓ | — | — |
| `clients_get` | ✓ | ✓ | ✓ | ✓ | — | — |
| `clients_conflict_check` | ✓ | ✓ | — | ✓ | — | — |

### Contacts

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `contacts_search` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `contacts_get` | ✓ | ✓ | ✓ | ✓ | ✓ | — |

### Billing

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `billing_time_entries_log` | ✓ | ✓ *own only* | ✓ *own only* | — | — | — |
| `billing_time_entries_search` | ✓ | ✓ *own only* | ✓ *own only* | — | — | — |
| `billing_get_summary` | ✓ | — | — | — | — | — |
| `billing_invoices_search` | ✓ | — | — | — | — | — |
| `billing_invoices_get` | ✓ | — | — | — | — | — |

### Calendar

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `calendar_get_hearings` | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| `calendar_get_deadlines` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `calendar_add_event` | ✓ | ✓ | — | — | — | — |
| `calendar_get_conflicts` | ✓ | ✓ | ✓ | — | — | — |

### Research

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `research_search_precedents` | ✓ | ✓ | ✓ | — | — | — |
| `research_get_statute` | ✓ | ✓ | ✓ | — | — | — |
| `research_get_memo` | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| `research_create_memo` | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| `research_search_memos` | ✓ | ✓ | ✓ | ✓ | — | ✓ |

### Intake

| Tool | Partner | Associate | OfCounsel | Paralegal | LegalAssistant | Intern |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `intake_create_request` | ✓ | ✓ | — | ✓ | ✓ | — |
| `intake_get_request` | ✓ | ✓ | — | ✓ | ✓ | — |
| `intake_run_conflict_check` | ✓ | ✓ | — | ✓ | — | — |
| `intake_approve` | ✓ | — | — | — | — | — |
| `intake_generate_engagement_letter` | ✓ | ✓ | — | — | — | — |

---

## Extending the System

### Adding a new tool

1. Add a `const string` to `McpToolName` in the appropriate nested class:
   ```csharp
   public static class Cases
   {
       public const string Archive = "cases_archive";   // new
   }
   ```

2. Add `Name = McpToolName.Cases.Archive` to the `[McpServerTool]` attribute:
   ```csharp
   [McpServerTool(Name = McpToolName.Cases.Archive), Description("...")]
   public static string CasesArchive(...) { ... }
   ```

3. Add the constant to each role's set in `ToolPermissionMatrix` that should have access. If a role is not updated, the tool is denied for that role automatically.

### Changing a role's permissions

Edit the relevant `HashSet<string>` in `ToolPermissionMatrix`. Adding or removing a `McpToolName.X.Y` entry is all that's needed — the filter pipeline picks up the change at runtime (the matrix is static and initialized once at startup).

### Adding a new role

1. Add the value to the `FirmRole` enum in `Core/Models/FirmRole.cs`.
2. Add a new entry to the `Matrix` dictionary in `ToolPermissionMatrix`.
3. Update the Entra ID app role mapping in `UserContextResolutionMiddleware` (or the equivalent claim resolver) so the new role is recognized from JWT claims.

---

## Anonymous / stdio Pass-Through

When running on the stdio transport (or HTTP with `UseAuth=false`), `IFirmIdentityContext` is not registered in the request's service scope. Both filters detect this with a `null` check and return without filtering:

```csharp
var identity = context.Services?.GetService<IFirmIdentityContext>();
if (identity is null)
    return result; // pass through — anonymous caller
```

This means the stdio transport exposes all 35 tools with no access control. This is intentional for local development and MCP Inspector use. Production deployments should always use HTTP transport with `UseAuth=true`. In fact CI-CD pipelines should be made to fail the build if UseAuth setting is detected or if the transport is stdio, to prevent accidental deployments with open access. The config reads the value in and defaults to the expected prod value to ensure this can't be set to an unsafe value without an explicit conscious decision.
