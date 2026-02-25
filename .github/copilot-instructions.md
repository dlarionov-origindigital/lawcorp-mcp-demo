# GitHub Copilot Instructions

> **See `CLAUDE.md` at the repo root for the full authoritative rule set.**
> This file contains the most relevant conventions for code completion and suggestions.

---

## Project

Law-Corp MCP Server — .NET 9 + Model Context Protocol + Entra ID OBO auth.

**Solution:** `src/LawCorp.Mcp.sln`

| Project | Purpose |
|---|---|
| `LawCorp.Mcp.Core` | Domain models, interfaces, `McpToolName` constants |
| `LawCorp.Mcp.Data` | EF Core DbContext + migrations |
| `LawCorp.Mcp.Server` | MCP server (HTTP + stdio) |
| `LawCorp.Mcp.Web` | Blazor Web App (MCP client) |

---

## Critical Code Conventions

### No Magic Strings for Tool Names

All MCP tool names live in `src/LawCorp.Mcp.Core/McpToolName.cs`. Always use constants:

```csharp
// ✅ CORRECT
[McpServerTool(Name = McpToolName.Cases.Search), Description("...")]
public static string CasesSearch(...) { ... }

// ❌ WRONG
[McpServerTool(Name = "cases_search"), Description("...")]
public static string CasesSearch(...) { ... }
```

When adding a new tool:
1. Add `const string` to the appropriate nested class in `McpToolName`
2. Use `[McpServerTool(Name = McpToolName.X.Y)]` on the method
3. Add the tool to `ToolPermissionMatrix` for each role that should have access

### Permission Matrix

`ToolPermissionMatrix.cs` maps each `FirmRole` to a set of permitted tool names using `McpToolName` constants. Never use raw string literals there.

### EF Core

- Never set explicit IDs for operational entities — let EF Core manage IDENTITY columns
- Use `ValueGeneratedNever()` only for reference/lookup data
- Always load navigation properties explicitly in LINQ before accessing them

### Config

- Never hardcode connection strings or secrets in source files
- Use `appsettings.Development.json` (gitignored) + `appsettings.Development.json.example` (tracked)

---

## Git

**AI assistants do NOT create git commits.** Implement code, verify it works, and hand off to the developer to commit.

---

## Project Management

Work items live in `proj-mgmt/epics/NN-slug/N.M.P-slug.md`. Always read the item file before implementing. Update `**Status:**` and check off acceptance criteria when done.
