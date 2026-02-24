# ADR-001: Use stdio transport for initial MCP server

**Status:** Superseded by [ADR-004](./004-dual-transport-web-api-primary.md)
**Date:** 2026-02-23

## Context

The MCP protocol supports multiple transport mechanisms. The two primary options for a .NET implementation are:

- **stdio** — the server communicates over stdin/stdout. The client (e.g., Claude Desktop, VS Code MCP extension) launches the server as a child process.
- **HTTP/SSE** — the server exposes an HTTP endpoint. Clients connect over the network, enabling multi-client and remote scenarios.

The initial project goal is a local development reference implementation. Deployment to Azure Foundry is a later-phase concern (Epic 6).

## Decision

Use **stdio transport** for the initial implementation via `WithStdioServerTransport()` in the `ModelContextProtocol` SDK.

The `mcp.json` at the repo root is configured to launch the server as a local process, which is the standard integration point for Claude Desktop and MCP-compatible editors.

## Consequences

**Easier:**
- Zero networking configuration for local development
- Direct integration with Claude Desktop via `mcp.json`
- Single-process debugging

**Harder:**
- Multi-client scenarios not possible with stdio
- Remote deployment (Azure Foundry) will require switching to HTTP transport in Epic 6
- Load testing is not straightforward over stdio

**Open questions:**
- When switching to HTTP for deployment, will the tool/resource/prompt implementations need changes, or only the transport registration in `Program.cs`? The SDK is designed to be transport-agnostic, so the answer should be "only `Program.cs`" — this should be validated before Epic 6.
