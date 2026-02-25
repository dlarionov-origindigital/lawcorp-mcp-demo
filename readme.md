# Law-Corp MCP Server

A Model Context Protocol (MCP) server for a law firm practice management system, built with .NET 9 and the official C# MCP SDK. This is a research and reference implementation exploring MCP capabilities including tools, resources, prompts, and sampling.

## What This Is

The Law-Corp MCP server exposes a fictional law firm's data and workflows to an LLM client (e.g. Claude Desktop). It demonstrates:

- **Tools** — structured actions the LLM can invoke (search cases, draft documents, log time, run conflict checks)
- **Role-based tool access** — `tools/list` is filtered per caller role; denied `tools/call` requests receive a structured error before any handler runs
- **Resources** — firm data exposed as readable URIs (`lawcorp://cases/...`, `lawcorp://documents/...`)
- **Prompts** — reusable prompt templates for common legal tasks
- **Sampling** — server-initiated LLM calls for AI-assisted enrichment (document classification, deadline extraction)

## Quick Start

**Prerequisites:** .NET 9 SDK, SQL Server Express (local)

```bash
# Build the solution
dotnet build src/LawCorp.Mcp.sln

# Run the MCP server (stdio transport)
dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

**Test via stdio** — send raw JSON-RPC to verify the echo tool works:

```bash
printf '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}\n{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}\n{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"Echo","arguments":{"message":"hello"}}}\n' \
  | timeout 15 dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server 2>/dev/null
```

**Connect to Claude Desktop** — add to your `claude_desktop_config.json`:

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

See [`src/README.md`](./src/README.md) for full project structure and setup instructions.
See [`src/LawCorp.Mcp.Web/README.md`](./src/LawCorp.Mcp.Web/README.md) for the Blazor web app (MCP client demo and E2E test harness).
See [`docs/local-dev.md`](./docs/local-dev.md) for the complete local development guide (MCP Inspector, Claude Desktop, VS Code/Cursor, Claude Code).
See [`docs/auth-config.md`](./docs/auth-config.md) for Microsoft Entra ID authentication setup (Azure app registration, appsettings, OBO token exchange).
See [`docs/local-mcp-inspect-auth.md`](./docs/local-mcp-inspect-auth.md) for testing authentication locally with the MCP Inspector and real Entra ID personas.
See [`docs/tool-permissions.md`](./docs/tool-permissions.md) for the role-to-tool permission matrix, how the MCP filter pipeline enforces it, and how to extend it.

## Repo Layout

| Folder | Purpose |
|---|---|
| [`src/`](./src/README.md) | .NET solution — MCP server, web app, domain model, data layer, mock data, tests |
| [`docs/`](./docs/README.md) | Research notes, reference material, and [local dev guide](./docs/local-dev.md) |
| [`proj-mgmt/`](./proj-mgmt/README.md) | Epics, features, stories, tasks, and architectural decisions |

## Tech Stack

- **.NET 9** — Dual transport: Generic Host (stdio) + ASP.NET Core WebApplication (HTTP)
- **Blazor Web App** — Interactive Server render mode, Fluent UI, Entra ID OIDC ([README](./src/LawCorp.Mcp.Web/README.md))
- **ModelContextProtocol 1.0.0-rc.1** — Official C# MCP SDK + ASP.NET Core integration
- **Entity Framework Core 9** — ORM with SQL Server Express
- **Microsoft Entra ID** — JWT Bearer auth + OBO flow (HTTP transport), OIDC (web app)
- **Fluent UI Blazor 4** — Enterprise component library with theming and white-labelling
- **xUnit** — Test framework