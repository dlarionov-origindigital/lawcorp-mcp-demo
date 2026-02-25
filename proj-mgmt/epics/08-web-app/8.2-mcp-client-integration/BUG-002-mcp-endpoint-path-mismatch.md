# BUG-002: MCP server endpoint path mismatch — tools page 404

**Status:** OPEN
**Type:** Bug
**Feature:** [8.2: MCP Client Integration](./8.2-mcp-client-integration.md)
**Severity:** High — blocks all MCP client functionality in the web app
**Tags:** `+web-app` `+mcp-client` `+server` `+bug`

---

## Summary

The MCP Tools page displays "Could not load tools: Response status code does not indicate success: 404 (Not Found)." because the web app sends requests to `http://localhost:5000/mcp` but the MCP server maps its HTTP endpoint at the root path `/`.

## Steps to Reproduce

1. Start the MCP server in HTTP mode: set `Transport: http` in `appsettings.Development.json`, then `dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server`
2. Start the web app: `dotnet run --project src/LawCorp.Mcp.Web --launch-profile https`
3. Sign in via Entra ID
4. Navigate to `/tools`
5. **Result:** "Could not load tools: Response status code does not indicate success: 404 (Not Found)."

## Expected Behaviour

The tools page should display the list of MCP tools from the server.

## Root Cause

**Server** (`ServerBootstrap.cs` line 81): `app.MapMcp()` is called with no route pattern argument. The `ModelContextProtocol.AspNetCore` SDK defaults to the root path `""`, mapping endpoints at:
- `POST /` — JSON-RPC messages
- `GET /` — SSE stream
- `DELETE /` — session cleanup

**Web app** (`appsettings.json` and `appsettings.Development.json`): `McpServer:Endpoint` is configured as `http://localhost:5000/mcp`. The `HttpClientTransport` sends `POST http://localhost:5000/mcp`, which returns 404 because nothing is mapped at `/mcp`.

## Recommended Fix

**Option A (preferred): Change the server to match the documented path**

In `ServerBootstrap.cs`, change:
```csharp
app.MapMcp();
```
to:
```csharp
app.MapMcp("/mcp");
```

This is preferred because:
- `http://localhost:5000/mcp` is already documented in `docs/auth-config.md` and `docs/local-mcp-inspect-auth.md`
- Both `appsettings.json` files in the web project reference `/mcp`
- A sub-path keeps the MCP endpoint distinct from any future health/status endpoints on the server

**Option B: Change the web app config to drop `/mcp`**

Change `McpServer:Endpoint` to `http://localhost:5000` in both `appsettings.json` and `appsettings.Development.json`. Less desirable because it contradicts existing documentation.

## Affected Files

- `src/LawCorp.Mcp.Server/ServerBootstrap.cs` — endpoint registration
- `src/LawCorp.Mcp.Web/appsettings.json` — endpoint config (committed)
- `src/LawCorp.Mcp.Web/appsettings.Development.json` — endpoint config (gitignored)
- `docs/auth-config.md` — documents `http://localhost:5000/mcp` as the endpoint

## Verification

After fix, navigate to `/tools` while signed in and with the MCP server running in HTTP mode. The tool list should load successfully.
