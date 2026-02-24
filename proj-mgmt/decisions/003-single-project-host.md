# ADR-003: Host MCP server as a single-process console application

**Status:** Superseded by [ADR-004](./004-dual-transport-web-api-primary.md)
**Date:** 2026-02-23

## Context

The MCP server needs a host process. Options considered:

- **Console app (Generic Host)** — `Microsoft.Extensions.Hosting` with `Host.CreateApplicationBuilder`. Simple, well-understood, matches the stdio transport model.
- **ASP.NET Core Web API** — full HTTP pipeline, controller-based or minimal API routing. Heavier setup, designed for HTTP.
- **Worker Service** — background service host, similar to Generic Host but with service lifecycle hooks.

The PRD refers to a ".NET Web API" as the target architecture, anticipating the HTTP transport needed for Azure Foundry deployment. However, for the stdio transport phase, the full ASP.NET Core pipeline is unnecessary overhead.

## Decision

Use a **Generic Host console application** (`Host.CreateApplicationBuilder`) for the current stdio transport phase. The `ModelContextProtocol` SDK's `WithStdioServerTransport()` integrates directly with the Generic Host's `IHostedService` lifecycle.

When the project moves to HTTP transport in Epic 6, the host will be migrated to **ASP.NET Core** (`WebApplication.CreateBuilder`). The MCP SDK supports both hosts; the tool, resource, and prompt registrations remain unchanged — only the host builder and transport registration change.

The five-project solution structure (`LawCorp.Mcp.Server`, `Core`, `Data`, `MockData`, `Tests`) separates concerns correctly regardless of host type.

## Consequences

**Easier:**
- Minimal boilerplate for the current development phase
- stdio transport works naturally with a console app lifetime
- All DI registrations, middleware, and logging are still available via Generic Host

**Harder:**
- The migration to ASP.NET Core in Epic 6 is a deliberate but small breaking change to `Program.cs`
- Developers expecting a Web API project may be surprised by the console app structure until they read this ADR

**Open questions:**
- Should authentication middleware (Entra ID JWT Bearer) be added to the Generic Host now, or deferred until the ASP.NET Core migration? Adding it now keeps the auth work in Epic 1 but may require rework during the host migration. Current recommendation: defer to Epic 6 and use a development bypass for local testing.
