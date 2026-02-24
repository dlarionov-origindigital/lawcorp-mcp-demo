# Law-Corp MCP Server

A Model Context Protocol (MCP) server for a law firm practice management system, built with .NET 9 and the official C# MCP SDK. This is a research and reference implementation exploring MCP capabilities including tools, resources, prompts, and sampling.

## What This Is

The Law-Corp MCP server exposes a fictional law firm's data and workflows to an LLM client (e.g. Claude Desktop). It demonstrates:

- **Tools** — structured actions the LLM can invoke (search cases, draft documents, log time, run conflict checks)
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
See [`docs/local-dev.md`](./docs/local-dev.md) for the complete local development guide (MCP Inspector, Claude Desktop, VS Code/Cursor, Claude Code).

## Repo Layout

| Folder | Purpose |
|---|---|
| [`src/`](./src/README.md) | .NET solution — MCP server, domain model, data layer, mock data, tests |
| [`docs/`](./docs/README.md) | Research notes, reference material, and [local dev guide](./docs/local-dev.md) |
| [`proj-mgmt/`](./proj-mgmt/README.md) | Epics, features, stories, tasks, and architectural decisions |

## Tech Stack

- **.NET 9** — Generic Host, stdio transport
- **ModelContextProtocol 0.9.0-preview.2** — Official C# MCP SDK
- **Entity Framework Core 9** — ORM with SQL Server Express
- **Microsoft Entra ID** — Planned auth via OBO flow
- **xUnit** — Test framework

## Development Rules

- Never commit directly — all commits are made by the developer
- See [`CLAUDE.md`](./CLAUDE.md) for Claude Code working instructions
- See [`CLAUDE-WORKFLOW.md`](./CLAUDE-WORKFLOW.md) for Claude agent workflow rules
- See [`COPILOT-RULES.md`](./COPILOT-RULES.md) for GitHub Copilot and team AI assistant rules
- See [`proj-mgmt/README.md`](./proj-mgmt/README.md) for how work is tracked and organized
