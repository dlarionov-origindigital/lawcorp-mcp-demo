# Research & Plan: BUG-002 — Tools page 404

**Status:** COMPLETE — Fix applied (Option A)
**Date:** 2026-02-25
**Bug:** [BUG-002: Tools page returns 404](./BUG-002-tools-page-404.md)

---

## 1. Investigation

### Request flow traced

1. User navigates to `/tools` → Blazor renders `Tools.razor`.
2. `OnInitializedAsync()` calls `McpClient.ListToolsAsync()`.
3. `McpClientService.EnsureClientAsync()` reads endpoint from config:
   - `AppConfigKeys.McpServer.Endpoint` → `"McpServer:Endpoint"` → `"http://localhost:5000/mcp"`
   - Fallback: `AppConfigKeys.McpServer.DefaultEndpoint` → `"http://localhost:5000/mcp"`
4. Creates `HttpClientTransport` with `Endpoint = new Uri("http://localhost:5000/mcp")`.
5. SDK sends `POST http://localhost:5000/mcp` with a JSON-RPC `tools/list` request.
6. MCP Server receives the request. ASP.NET Core routing has no handler for `/mcp` → **404**.

### Server endpoint mapping

`ServerBootstrap.RunHttpAsync()` calls `app.MapMcp()` with no arguments (line 87). The `ModelContextProtocol.AspNetCore` SDK's `MapMcp()` overload with no parameters registers the MCP Streamable HTTP endpoint at the **root path** `""`:

| HTTP Method | Path | Purpose |
|---|---|---|
| `POST` | `/` | JSON-RPC message endpoint |
| `GET` | `/sse` | SSE event stream |
| `DELETE` | `/` | Session cleanup |

### Documentation vs. reality

Every reference in the codebase and docs assumes the endpoint lives at `/mcp`:

| Location | Value |
|---|---|
| `docs/local-dev.md` line 80 | `http://localhost:5000/mcp` |
| `docs/local-dev.md` line 284 | "MCP endpoint is exposed at `http://localhost:5000/mcp`" |
| `docs/local-mcp-inspect-auth.md` line 168 | "`app.MapMcp()` exposes the MCP endpoint at `/mcp`" |
| `docs/local-mcp-inspect-auth.md` line 206 | `curl` example targets `/mcp` |
| `docs/local-mcp-inspect-auth.md` line 292 | MCP Inspector URL: `http://localhost:5000/mcp` |
| `docs/local-mcp-inspect-auth.md` line 411 | FAQ: "The MCP endpoint is at `/mcp`, not the root `/`" |
| `src/LawCorp.Mcp.Web/appsettings.json` line 19 | `"Endpoint": "http://localhost:5000/mcp"` |
| `src/LawCorp.Mcp.Web/AppConfigKeys.cs` line 21 | `DefaultEndpoint = "http://localhost:5000/mcp"` |
| `proj-mgmt/decisions/006-web-app-architecture.md` line 64 | ADR example: `new Uri("http://localhost:5000/mcp")` |
| `proj-mgmt/epics/08-web-app/8.2-mcp-client-integration/8.2.1-mcp-client-service.md` line 28 | Config example: `/mcp` |

The server code is the only place that disagrees — `MapMcp()` maps to `/`.

---

## 2. Root Cause

`app.MapMcp()` in `ServerBootstrap.cs` is called without a route prefix argument. The SDK default is `""` (root), but every consumer and document assumes `/mcp`.

---

## 3. Fix Options

### Option A — Add `/mcp` prefix on the server (recommended)

**Change:** `ServerBootstrap.cs` line 87

```csharp
// Before
app.MapMcp();

// After
app.MapMcp("/mcp");
```

**Pros:**
- One-line change in one file.
- Matches all existing configuration (web app `appsettings.json`, `AppConfigKeys.cs`).
- Matches all documentation (`local-dev.md`, `local-mcp-inspect-auth.md`, ADR-006).
- Keeps the MCP endpoint on a distinct sub-path, leaving room for health checks and OpenAPI at other paths.

**Cons:**
- None for current state. Future story 6.5.3 plans to move to `/api/mcp`, but that's an intentional breaking change for App Service deployment — not a concern now.

**Files touched:** 1
- `src/LawCorp.Mcp.Server/ServerBootstrap.cs`

### Option B — Change web app config to drop `/mcp`

**Change:** Update `McpServer:Endpoint` to `http://localhost:5000` in:
- `src/LawCorp.Mcp.Web/appsettings.json`
- `src/LawCorp.Mcp.Web/appsettings.Development.json`
- `src/LawCorp.Mcp.Web/AppConfigKeys.cs` (default endpoint)

**Pros:**
- No server-side change.

**Cons:**
- Contradicts all documentation.
- Requires updating multiple web app files.
- MCP endpoint on root conflicts with future health check and OpenAPI endpoints (see RESEARCH-mcp-server-custom-endpoints.md).
- MCP Inspector users would need to target root URL instead of documented `/mcp` path.
- Story 6.5.3 would later move to `/api/mcp` anyway, making this a throwaway change.

### Option C — Change both to `/api/mcp` (accelerate 6.5.3)

**Change:** Set `MapMcp("/api/mcp")` on server, update web app config and all docs to `http://localhost:5000/api/mcp`.

**Pros:**
- Reaches the target state from story 6.5.3 immediately.
- Follows Microsoft's App Service tutorial pattern.

**Cons:**
- Scope creep — this bug fix becomes a broader config migration.
- Requires updating docs, MCP Inspector instructions, Claude Desktop config examples, VS Code config examples.
- Should be done as story 6.5.3, not as a bug fix.

---

## 4. Recommendation

**Go with Option A.** It is a single-line server change that aligns the runtime with every existing consumer and document. No config files, no docs, no client code needs to change.

Story 6.5.3 can later migrate from `/mcp` to `/api/mcp` as a deliberate, tracked change when App Service deployment is in scope.

### Implementation steps

1. In `src/LawCorp.Mcp.Server/ServerBootstrap.cs`, change line 87:
   ```csharp
   app.MapMcp("/mcp");
   ```
2. Verify: start the MCP server (`dotnet run --project src/LawCorp.Mcp.Server`), navigate to `/tools` in the web app. Tools should load.
3. Verify: MCP Inspector connecting to `http://localhost:5000/mcp` works.
4. Mark BUG-002 as FIXED.
5. Update story 8.2.3 acceptance criteria checks.

### Estimated effort

Trivial — single line change, ~5 minutes including verification.

---

## 5. Related items

| Item | Relationship |
|---|---|
| [6.5.3: Move MCP Endpoint to `/api/mcp`](../../../../06-protocol-deployment/6.5-health-observability/6.5.3-mcp-endpoint-path.md) | Future migration from `/mcp` → `/api/mcp`. Option A does not conflict. |
| [RESEARCH: MCP Server Custom Endpoints](../../../../01-foundation/1.1-solution-structure/RESEARCH-mcp-server-custom-endpoints.md) | Confirms `/api/mcp` as long-term target, health checks need non-root paths. |
| [8.2.1: MCP Client Service](../../8.2.1-mcp-client-service.md) | Defines the client config that expects `/mcp`. |
| [docs/local-mcp-inspect-auth.md](../../../../../docs/local-mcp-inspect-auth.md) | Documents `/mcp` as the endpoint path. |
