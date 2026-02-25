# BUG-002: Tools page returns 404 — MCP endpoint path mismatch

**Status:** FIXED
**Type:** Bug
**Story:** [8.2.3: Tool discovery and invocation UI](../8.2.3-tool-invocation-ui.md)
**Feature:** [8.2: MCP Client Integration](../../8.2-mcp-client-integration.md)
**Severity:** High — blocks all MCP client functionality in the web app
**Tags:** `+web-app` `+mcp-client` `+server` `+bug`

---

## Summary

Navigating to `/tools` in the Blazor web app displays:

> Could not load tools: Response status code does not indicate success: 404 (Not Found).

The web app's `McpClientService` sends HTTP requests to `http://localhost:5000/mcp`, but the MCP server maps its Streamable HTTP endpoint at the root path `/` because `app.MapMcp()` is called with no route prefix argument.

## Steps to Reproduce

1. Start the MCP server in HTTP mode (`Transport: http` in `appsettings.Development.json`):
   `dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server`
2. Start the web app:
   `dotnet run --project src/LawCorp.Mcp.Web --launch-profile https`
3. Navigate to `/tools`
4. **Result:** "Could not load tools: Response status code does not indicate success: 404 (Not Found)."

## Expected Behaviour

The tools page loads and displays the list of MCP tools from the server.

## Root Cause

| Side | File | What happens |
|------|------|--------------|
| **Server** | `ServerBootstrap.cs` line 87 | `app.MapMcp()` — no route prefix. SDK maps `POST /`, `GET /sse`, `DELETE /` at root. |
| **Web app** | `appsettings.json` line 19 | `McpServer:Endpoint` = `http://localhost:5000/mcp` |
| **Web app** | `AppConfigKeys.cs` line 21 | `DefaultEndpoint` = `http://localhost:5000/mcp` |
| **Web app** | `McpClientService.cs` line 41 | Reads endpoint from config → `HttpClientTransport` POSTs to `/mcp` |

The `HttpClientTransport` sends `POST http://localhost:5000/mcp`. Nothing is mapped at `/mcp`, so the server returns **404**.

## Affected Files

- `src/LawCorp.Mcp.Server/ServerBootstrap.cs` — endpoint registration
- `src/LawCorp.Mcp.Web/appsettings.json` — committed endpoint config
- `src/LawCorp.Mcp.Web/appsettings.Development.json` — gitignored endpoint config
- `src/LawCorp.Mcp.Web/AppConfigKeys.cs` — hardcoded default endpoint
- `docs/local-dev.md` — documents `/mcp` as the endpoint path
- `docs/local-mcp-inspect-auth.md` — documents `/mcp` as the endpoint path

## Related Items

- [RESEARCH: Root cause and fix plan](./RESEARCH-tools-page-404.md)
- [6.5.3: Move MCP Endpoint to `/api/mcp`](../../../../06-protocol-deployment/6.5-health-observability/6.5.3-mcp-endpoint-path.md) — longer-term story to move to `/api/mcp` for App Service deployment

## Verification

After fix: navigate to `/tools` while signed in (or in demo mode) with the MCP server running in HTTP mode. The tool list should load and display all registered tools.
