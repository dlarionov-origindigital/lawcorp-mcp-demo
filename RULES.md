# Project Rules

> **Canonical rules file.** This is the single source of truth for AI agent behavior on this project.
> All AI agents should follow these rules. Agent-specific config files (`.cursor/rules/`, `.github/copilot-instructions.md`) replicate the essentials but this file is authoritative.

---

## Git Rules

**CRITICAL: AI assistants do NOT create git commits. Ever.**

- Implement code changes
- Update `.md` files with completion status and code references
- Verify everything works
- Hand off to developer for commit

The developer decides when/how to commit based on logical units of work.

---

## Communication Preferences

- **When SDK/library internals are needed and not easily readable (e.g., decompiling DLLs, obscure NuGet internals): ask the user directly.** Do not attempt to run decompilers or other binary inspection tools. A direct question is always preferred over a tool rabbit hole.
- **Default to updating local project rules**, not global user rules. When the user asks to save a rule, update this file (`RULES.md`)
- **User will specify when a rule should be global.** If not specified, it goes in the project.

---

## Project Overview

Law-Corp MCP Server — .NET 9 enterprise reference architecture demonstrating the Model Context Protocol (MCP) with Microsoft Entra ID On-Behalf-Of (OBO) auth.

**Solution root:** `src/`

| Project | Purpose |
|---|---|
| `LawCorp.Mcp.Core` | Domain models, interfaces, `McpToolName` constants (no ASP.NET dependency) |
| `LawCorp.Mcp.Data` | EF Core DbContext + migrations |
| `LawCorp.Mcp.MockData` | Seed data generator |
| `LawCorp.Mcp.Server` | MCP server (HTTP + stdio transport) |
| `LawCorp.Mcp.Web` | Blazor Web App (MCP client, OBO flow) |
| `LawCorp.Mcp.Tests` | Unit tests |
| `LawCorp.Mcp.Tests.E2E` | Playwright E2E |

**Key files:**
- `src/LawCorp.Mcp.Core/McpToolName.cs` — all MCP tool name constants
- `src/LawCorp.Mcp.Core/Auth/IFirmIdentityContext.cs` — domain identity interface
- `src/LawCorp.Mcp.Core/Auth/IToolPermissionPolicy.cs` — RBAC policy interface
- `src/LawCorp.Mcp.Server/ServerBootstrap.cs` — transport configuration (HTTP / stdio)
- `src/LawCorp.Mcp.Server/Auth/ToolPermissionMatrix.cs` — role-to-tool permission matrix
- `src/LawCorp.Mcp.Server/Auth/ToolPermissionFilters.cs` — MCP request filter pipeline hooks
- `proj-mgmt/` — project management (epics, features, stories, bugs)
- `docs/agentic-engineering.md` — guide for AI-assisted development on this project

**Key types:**
- `IFirmIdentityContext` — domain identity (role, practice group, assigned cases)
- `IUserContext` — basic caller info (UserId, DisplayName, Role, IsPartner, IsAttorney)
- `FirmRole` enum — `Partner, Associate, OfCounsel, Paralegal, LegalAssistant, Intern`
- `ServerBootstrap` — `RunHttpAsync` / `RunStdioAsync`
- `AuthServiceCollectionExtensions.AddEntraIdAuthentication()` — registers auth + scoped identity

**Auth/transport model:**
- HTTP transport: Entra ID JWT → `UserContextResolutionMiddleware` → `IFirmIdentityContext` in scoped DI
- stdio transport: anonymous fallback (`AnonymousUserContext`); `UseAuth=true` logs a warning and ignores
- Transport selected via `Transport` appsettings key (`http` or `stdio`, default `stdio`)

**SDK:** `ModelContextProtocol` 1.0.0-rc.1 + `ModelContextProtocol.AspNetCore` 1.0.0-rc.1

---

## Code Implementation Rules

1. **No magic strings for tool names.** All MCP tool name strings are defined once in `LawCorp.Mcp.Core/McpToolName.cs` as `const string` fields in nested static classes grouped by domain (e.g. `McpToolName.Cases.Search = "cases_search"`). Reference these constants everywhere: in `[McpServerTool(Name = McpToolName.Cases.Search)]` on the method, and in the permission matrix. Never write a raw `"cases_search"` literal in code.

2. **`McpServerToolAttribute.Name` must always be set explicitly.** Use `[McpServerTool(Name = McpToolName.X.Y)]` — do not rely on the SDK's PascalCase→snake_case convention. The `Name` property is the binding between the constant and the registered tool name.

3. **No explicit IDs.** Let EF Core manage IDENTITY columns for operational data. Only use `ValueGeneratedNever()` for reference data (PracticeGroups, lookup tables, etc.).

4. **Foreign keys.** Reference data IDs (1-6, 1-5) are stable. Operational data IDs are auto-generated; always save before querying related data.

5. **Null checks.** Verify navigation properties are populated before accessing. Load related entities explicitly in LINQ queries.

6. **Tests first.** Write regression tests before marking a bug DONE. Test both the fix and that the bug doesn't reoccur.

7. **Config files.** Never hardcode values. Use `appsettings.Development.json` + `appsettings.Development.json.example` pattern.

8. **Migrations.** If modifying the schema, create migrations but don't run them in `Program.cs` unless explicitly needed. Use `EnsureCreatedAsync()` for dev/testing only.

---

## MCP-Specific Rules

### Tool Name Constants

All MCP tool names live in `src/LawCorp.Mcp.Core/McpToolName.cs`. When adding a new tool:
1. Add a `const string` to the appropriate nested class in `McpToolName`
2. Set `Name = McpToolName.X.Y` in `[McpServerTool]` on the method
3. Add the tool to `ToolPermissionMatrix` for each role that should have access

### Permission Matrix

Role-to-tool authorization is defined in `ToolPermissionMatrix.cs`. Each `FirmRole` maps to an `IReadOnlySet<string>` of permitted tool names. The filter pipeline hooks (`ToolPermissionFilters.cs`) enforce this on every `tools/list` and `tools/call` request.

Anonymous/stdio callers (null `IFirmIdentityContext`) pass through the filter without restriction.

### MCP Filter API (SDK 1.0.0-rc.1)

```csharp
builder.Services.AddMcpServer()
    .WithToolsFromAssembly()
    .WithRequestFilters(f => {
        f.AddListToolsFilter(ToolPermissionFilters.ListTools);
        f.AddCallToolFilter(ToolPermissionFilters.CallTool);
    });
```

- `McpRequestFilter<TParams, TResult>` = `McpRequestHandler<TParams, TResult> Filter(McpRequestHandler<TParams, TResult> next)`
- `McpRequestHandler<TParams, TResult>` = `ValueTask<TResult> Handler(RequestContext<TParams> request, CancellationToken ct)`
- `RequestContext<T>` inherits `MessageContext.Services: IServiceProvider?` (scoped when `ScopeRequests=true`)
- `ListToolsResult.Tools` is `IList<Tool>` (sealed class — use `.Clear()` + `.Add()` to mutate, no `with`)
- `CallToolResult` is sealed with `Content: IList<ContentBlock>` and `IsError: bool?`

---

## Project Management Workflow

### Core Principles

1. **File is truth** — The `.md` file is the canonical definition of work. Always read it first, implement it, verify it, then update it.
2. **Never break hierarchy** — All work items must live in `proj-mgmt/epics/NN-*/` folder structure.
3. **One file, one concern** — Each `.md` file owns exactly one work item.
4. **Status before code** — Always check and understand the `**Status:**` field before starting work.
5. **Living history** — Every bug, decision, and change is documented and linked.

### File Structure

- **Epic folder** = `proj-mgmt/epics/NN-slug/_epic.md` (e.g., `01-foundation/_epic.md`)
- **Feature file** = `proj-mgmt/epics/NN-slug/N.M-slug.md` (e.g., `1.3-authorization.md`)
- **Item file** = `proj-mgmt/epics/NN-slug/N.M.P-slug.md` (e.g., `1.3.1-role-based-handler.md`)
- **Bug (simple)** = same convention, `Type: Bug`
- **Bug (complex)** = subfolder `N.M.P-slug/bugs/N.M.P.X-bug.md`

### Pre-Work Checklist (Before Starting Any Implementation)

- [ ] Read the item file completely — don't skim
- [ ] Check status — if `IN PROGRESS` or `DONE`, ask before overwriting
- [ ] Verify hierarchy — confirm item is in correct epic/feature folder
- [ ] Check dependencies — read "Blocks" and "Depends on" fields
- [ ] Review acceptance criteria — these define "done"
- [ ] Check related files — linked ADRs, parent feature, epic
- [ ] Understand the domain — read related PRD sections if unfamiliar

### Implementation Phases

**Phase 1: Understanding** — read story → feature → epic → ADRs → PRD

**Phase 2: Implementation** — code → verify each criterion → no regressions → document choices → tests

**Phase 3: Completion** — check off all criteria → update `Status` to `DONE` → add code references → update parent feature/epic tables

**Phase 4: Documentation** — ADR if non-obvious choice → close PRD open questions → link related items

### When Starting New Work

**Specific item given** ("Implement 1.3.1"):
1. Navigate to `proj-mgmt/epics/01-foundation/1.3.1-*.md`
2. Run Pre-Work Checklist
3. Change status `BACKLOG` → `IN PROGRESS`
4. Implement per phases above

**Vague request given** ("Add validation for..."):
1. Run Systems Thinking Checklist
2. Find or create the item file in the correct epic
3. Draft the item and ask for approval before implementing

### Status Rules

**Never:**
- Set to `DONE` without verifying ALL acceptance criteria
- Set to `DONE` without linked code/tests
- Set an epic to `DONE` if any sub-item is not `DONE`

**Always:**
- Provide context when marking `BLOCKED`
- Update parent feature/epic status tables
- Link related items in "Notes" section

### Systems Thinking Checklist

Before designing a solution:

**1. Domain Boundary**
- What entities does this read/write?
- What other features touch these entities?
- Does this violate DDD bounded contexts?

**2. Authorization Impact**
- Which roles need to execute this?
- What data is privileged or needs redaction for lower roles?
- Does the role matrix in PRD Section 4.2 cover this?

**3. Data Access Pattern**
- Which entities need row-level filtering?
- What should be audit-logged?

**4. Protocol Surface**
- Is this a tool, resource, prompt, or cross-cutting feature?
- Does it need pagination, progress reporting, cancellation, or rate limits?

**5. Dependencies**
- What must exist before building this?
- What will break if this changes?
- Are there data migrations needed?

**6. Test Surface**
- Unit tests: business logic
- Integration tests: auth checks, happy path, error cases
- Manual test: end-to-end flow

**7. Decisions & Reversibility**
- Did I make any non-obvious choices?
- Would it be expensive to reverse?
- Should I write an ADR?

### Item Templates

**Story:**
```markdown
# N.M.P: Title

**Status:** BACKLOG
**Type:** Story
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`

---

As a [role],
I want [capability],
So that [value].

## Acceptance Criteria

- [ ] Criterion one
- [ ] Criterion two
```

**Bug:**
```markdown
# N.M.P: Bug Title

**Status:** BACKLOG
**Type:** Bug
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`, `+severity:high`

---

## Problem

**Expected:**
**Actual:**
**Reproduces:**

## Acceptance Criteria

- [ ] Bug no longer reproduces
- [ ] Regression test added
```

---

## When Uncertain

1. **Ask the user directly** — If SDK internals are opaque or requirements are ambiguous, ask. Don't guess or use obscure tooling.
2. **Reference the PRD** — If something seems contradictory, link the relevant PRD section.
3. **Draft an ADR** — If about to make a costly or non-obvious decision, draft an ADR and ask for approval before implementing.
4. **Run Systems Thinking** — If confused about scope, work through the checklist.
