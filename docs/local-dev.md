# Local Development Guide

This guide covers how to run, test, and debug the Law-Corp MCP server on your local machine using various MCP clients and the MCP Inspector.

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0+ | `dotnet --version` to verify |
| SQL Server Express | Local instance at `.\SQLEXPRESS` | Or update the connection string |
| [Node.js / npm](https://nodejs.org/) | 18+ | Required for MCP Inspector (`npx`) |

## Port Allocation

All services use distinct ports to avoid `Failed to bind to address: address already in use` errors.

| Service | HTTP Port | HTTPS Port | Project |
|---|---|---|---|
| MCP Server | 5000 | — | `LawCorp.Mcp.Server` |
| Web App | 5003 | 5001 | `LawCorp.Mcp.Web` |
| External API (DMS) | 5002 | 7002 | `LawCorp.Mcp.ExternalApi` |

Ports are configured via `Properties/launchSettings.json` in each project and Kestrel endpoint settings in `appsettings.Development.json`. If a port is in use, kill the process or change the port in both `launchSettings.json` and `appsettings.Development.json`.

**Troubleshooting port conflicts on Windows:**

```bash
# Find what's holding a port
netstat -ano | findstr ":5002"

# Kill it by PID
taskkill /PID <pid> /F
```

---

## Initial Setup

```bash
# 1. Build the solution
dotnet build src/LawCorp.Mcp.sln

# 2. Copy example configs for each service
cp src/LawCorp.Mcp.Server/appsettings.Development.json.example \
   src/LawCorp.Mcp.Server/appsettings.Development.json

cp src/LawCorp.Mcp.ExternalApi/appsettings.Development.json.example \
   src/LawCorp.Mcp.ExternalApi/appsettings.Development.json

# 3. Run the MCP server (stdio transport, default)
dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

The server will start in stdio mode, waiting for JSON-RPC messages on stdin. If `SeedMockData` is `true` in your `appsettings.Development.json`, the database is recreated and seeded on every startup.

> **Important:** Always use `--no-launch-profile` when running via stdio. Without it, `dotnet run` prints `"Using launch settings from..."` to stdout, which corrupts the JSON-RPC stream and causes MCP clients and the Inspector to fail with `SyntaxError: Unexpected token`. Visual Studio is unaffected because it launches the exe directly, bypassing `dotnet run`.

---

## Running All Services (Full Stack)

For document tools to work, both the MCP Server and the External API (DMS) must be running. Open separate terminals:

**Terminal 1 — External API (DMS):**

```bash
dotnet run --project src/LawCorp.Mcp.ExternalApi --launch-profile http
```

Starts at `http://localhost:5002`. Verify with `curl http://localhost:5002/health`.

**Terminal 2 — MCP Server (HTTP transport):**

```bash
dotnet run --project src/LawCorp.Mcp.Server --launch-profile http
```

Starts at `http://localhost:5000`. MCP endpoint at `http://localhost:5000/mcp`.

**Terminal 3 — Web App (optional):**

```bash
dotnet run --project src/LawCorp.Mcp.Web --launch-profile https
```

Starts at `https://localhost:5001`.

### Databases

The solution uses two separate SQL Server databases:

| Database | Service | Seed Setting | Content |
|---|---|---|---|
| `LawCorpLocal` | MCP Server | `"SeedMockData": true` | Cases, clients, users, billing, etc. |
| `LawCorpDms` | External API | `"SeedDmsData": true` | DMS workspaces, documents, access rules |

Both databases are created automatically via `EnsureCreatedAsync()` when seeding is enabled. No manual SQL setup is required.

### How tools route to data sources

Case tools (e.g., `cases_search`, `cases_get`) dispatch through MediatR to handlers that query the local `LawCorpLocal` database directly (in-process).

Document tools (e.g., `documents_search`, `documents_get`) dispatch through MediatR to handlers that make HTTP calls to the External API at `http://localhost:5002`, which queries the `LawCorpDms` database. When auth is enabled, these calls use OBO token exchange.

```
MCP Client (Claude Desktop / Inspector / Web App)
    │
    ▼
MCP Server (:5000)
    │
    ├─ cases_search   → MediatR → SearchCasesHandler   → LawCorpLocal DB (in-process)
    ├─ cases_get      → MediatR → GetCaseByIdHandler    → LawCorpLocal DB (in-process)
    │
    ├─ documents_search → MediatR → SearchDocumentsHandler → HTTP → External API (:5002) → LawCorpDms DB
    └─ documents_get    → MediatR → GetDocumentByIdHandler → HTTP → External API (:5002) → LawCorpDms DB
```

### OpenAPI & Swagger UI

Both HTTP services expose OpenAPI specs and an interactive Swagger UI (Scalar) in non-Production environments ([ADR-009](../proj-mgmt/decisions/009-swagger-ui-local-dev.md)):

| Service | Swagger UI (Scalar) | OpenAPI Spec |
|---|---|---|
| MCP Server | `http://localhost:5000/scalar/v1` | `http://localhost:5000/openapi/v1.json` |
| External API | `http://localhost:5002/scalar/v1` | `http://localhost:5002/openapi/v1.json` |

When launching with the `http` launch profile, the browser auto-opens to the Scalar UI. You can explore endpoints, view schemas, and send test requests directly from the browser.

> OpenAPI and Scalar UI are available in Development, QA, and UAT environments. They are **not** served in Production. Environment is controlled by `ASPNETCORE_ENVIRONMENT` / `DOTNET_ENVIRONMENT`.

---

## Connecting MCP Clients

### Claude Desktop

Add to your `claude_desktop_config.json`:

- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "law-corp": {
      "command": "dotnet",
      "args": ["run", "--no-launch-profile", "--project", "C:\\Dev\\research\\mcp\\src\\LawCorp.Mcp.Server"]
    }
  }
}
```

> **Important:** `--no-launch-profile` is required. Without it, `dotnet run` prints launch profile info to stdout, corrupting the stdio JSON-RPC stream (see bug [1.1.2.2](../proj-mgmt/epics/01-foundation/1.1.2-configure-mcp-skeleton/bugs/1.1.2.2-dotnet-run-launch-profile-corrupts-stdio.md)).

### VS Code / Cursor (Copilot Chat)

Create `.vscode/mcp.json` in the repo root (or use your user-level config):

```json
{
  "servers": {
    "law-corp": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--no-launch-profile", "--project", "src/LawCorp.Mcp.Server"]
    }
  }
}
```

VS Code will automatically discover and start the server when you open a chat session that uses MCP tools. You can manage servers via the command palette: **MCP: List Servers**.

See the [VS Code MCP configuration reference](https://code.visualstudio.com/docs/copilot/reference/mcp-configuration) for full options.

### Claude Code (CLI)

```bash
claude mcp add --transport stdio law-corp -- dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

See [Claude Code MCP docs](https://code.claude.com/docs/en/mcp) for details.

---

## MCP Inspector

The [MCP Inspector](https://modelcontextprotocol.io/docs/tools/inspector) is an interactive browser-based tool for testing and debugging MCP servers. It connects to your server, discovers its tools/resources/prompts, and lets you invoke them with custom inputs — without needing a full LLM client.

### Running the Inspector

The Inspector runs via `npx` with no installation required:

```bash
npx @modelcontextprotocol/inspector dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

This spawns the MCP server as a subprocess (stdio transport) and opens a web UI (typically at `http://localhost:6274`) where you can:

- **Tools tab** — list all registered tools, see their JSON schemas, invoke them with test inputs, and inspect the JSON-RPC responses
- **Resources tab** — browse exposed resources, inspect their content and MIME types
- **Prompts tab** — test prompt templates with custom arguments and preview the generated messages
- **Notifications pane** — view server logs and notifications in real time

### Inspector Workflow

1. **Verify connectivity** — launch the Inspector, confirm the server initializes and capabilities are negotiated
2. **Discover tools** — check that all expected tools appear in the Tools tab with correct schemas
3. **Test tools** — invoke a tool (e.g. `cases_search`) with sample arguments and verify the response shape
4. **Check edge cases** — test invalid inputs, missing arguments, and boundary conditions
5. **Iterate** — make code changes, rebuild the server, click "Reconnect" in the Inspector to pick up changes

### Passing Environment Variables

If you need to override configuration (e.g. disable mock data seeding):

```bash
npx @modelcontextprotocol/inspector \
  --env SeedMockData=false \
  -- dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

The Inspector passes environment variables through to the spawned server process.

### When to Use the Inspector vs. a Chat Client

| Scenario | Use Inspector | Use Claude Desktop / VS Code |
|---|---|---|
| Verify a tool's JSON schema and response format | Yes | |
| Test a specific tool with known inputs | Yes | |
| Debug JSON-RPC message flow | Yes | |
| Test how the LLM selects and chains tools | | Yes |
| End-to-end conversational workflows | | Yes |
| Demo to stakeholders | | Yes |

The Inspector is a **developer tool** — it shows you what the server exposes and lets you call it directly. Chat clients show you how an LLM **uses** what the server exposes.

### Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| `SyntaxError: Unexpected token 'U', "Using laun"...` | `dotnet run` prints launch profile info to stdout, corrupting JSON-RPC | Add `--no-launch-profile` to the `dotnet run` command. See bug [1.1.2.2](../proj-mgmt/epics/01-foundation/1.1.2-configure-mcp-skeleton/bugs/1.1.2.2-dotnet-run-launch-profile-corrupts-stdio.md). |
| Inspector shows "connection failed" | Server crashed on startup | Check the Inspector's stderr/log pane for the exception. Common cause: missing `appsettings.Development.json` — copy from `.example`. |
| Tools list is empty | Server started but tool registration failed | Check that the project builds cleanly (`dotnet build`). Look for assembly scanning errors in the log pane. |
| Inspector opens but shows a blank page | Node.js version too old | Upgrade to Node.js 18+. |
| Tool invocation returns an error | Database not seeded or connection failed | Ensure SQL Express is running and `SeedMockData=true` in config. |

### References

- [MCP Inspector — Official Docs](https://modelcontextprotocol.io/docs/tools/inspector)
- [MCP Inspector — GitHub](https://github.com/modelcontextprotocol/inspector)
- [MCP Debugging Guide](https://modelcontextprotocol.io/legacy/tools/debugging)

---

## Quick Verification

After setup, verify everything works end-to-end:

```bash
# 1. Build
dotnet build src/LawCorp.Mcp.sln

# 2. Send a raw JSON-RPC initialize + tools/list via stdio
printf '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}\n{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}\n{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}\n' \
  | timeout 15 dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server 2>/dev/null

# 3. Or use the Inspector for an interactive check
npx @modelcontextprotocol/inspector dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

---

## Transport Modes

The server supports two transport modes, configured by the `Transport` setting in `appsettings`:

| Transport | Host type | When to use |
|---|---|---|
| `stdio` (default) | Generic Host | MCP clients that launch the server as a subprocess (Claude Desktop, VS Code, Inspector stdio) |
| `http` | ASP.NET Core WebApplication | Browser-based testing, authenticated sessions, deployed environments |

Set `"Transport": "http"` in `appsettings.Development.json` to run with Kestrel. The MCP endpoint is exposed at `http://localhost:5000/mcp` (configurable via the `Kestrel` section).

## Authenticated Testing

When `UseAuth=true` and `Transport=http`, the server validates Entra ID Bearer tokens and resolves the caller's identity from the database. See the dedicated guide:

**[Testing Authentication with MCP Inspector](./local-mcp-inspect-auth.md)** — step-by-step instructions for acquiring persona tokens, connecting the Inspector with auth, and verifying that different identities produce different results.

See also [docs/auth-config.md](./auth-config.md) for the Azure app registration and appsettings setup that must be completed first.

---

## Related

- [src/README.md](../src/README.md) — project structure and build instructions
- [docs/auth-config.md](./auth-config.md) — Entra ID authentication setup
- [docs/local-mcp-inspect-auth.md](./local-mcp-inspect-auth.md) — auth testing with MCP Inspector
- [docs/arch/mcp.md](./arch/mcp.md) — MCP protocol overview
- [ADR-004](../proj-mgmt/decisions/004-dual-transport-web-api-primary.md) — dual transport decision (stdio + HTTP)
- [Transport research](../proj-mgmt/epics/01-foundation/1.1.2-configure-mcp-skeleton/RESEARCH-stdio-vs-http-transport.md) — stdio vs HTTP analysis
