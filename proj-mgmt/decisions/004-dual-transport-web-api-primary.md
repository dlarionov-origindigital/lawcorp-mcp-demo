# ADR-004: Adopt ASP.NET Core Web API as primary host; retain stdio as a configuration mode

**Status:** Accepted
**Date:** 2026-02-24
**Supersedes:** [ADR-001](./001-stdio-transport.md), [ADR-003](./003-single-project-host.md)

## Context

ADR-001 and ADR-003 established a **Generic Host console app with stdio transport** as the initial architecture, deferring the ASP.NET Core migration to Epic 6. Three new requirements make it preferable to front-load that migration now:

1. **End-to-end testing (Epic 7)** — Integration and E2E tests use `WebApplicationFactory<Program>`, which requires an ASP.NET Core host. Testing against stdio requires spawning a child process and parsing JSON-RPC over pipes — cumbersome and fragile. A Web API host lets tests call the server over HTTP using a standard `HttpClient` backed by an in-memory test server, with no process boundary.

2. **Supplemental endpoints** — Deployment to Azure Foundry needs a `/health` endpoint and will benefit from other operational HTTP endpoints (metrics, readiness). These are natural in ASP.NET Core but awkward to add to a Generic Host console app.

3. **Multi-client and remote scenarios** — The stdio transport is inherently single-client. The production target (Azure Foundry) requires HTTP. Moving to ASP.NET Core now removes a deliberate breaking change from Epic 6.

**Alternatives considered:**

- **Keep Generic Host + add a test adapter for stdio** — Possible but complex: tests must manage process lifetime, pipe encoding, and JSON-RPC framing. Every test becomes an integration test at the process boundary. Rejected.
- **Keep Generic Host + add a parallel ASP.NET Core test project** — Two separate hosts for the same server code. Maintenance overhead, divergence risk. Rejected.
- **Continue deferring to Epic 6** — The effort to migrate later is essentially the same as migrating now, but deferring means all test infrastructure built in Epic 7 is blocked until Epic 6. Rejected.

## Decision

Migrate the `LawCorp.Mcp.Server` host from `Host.CreateApplicationBuilder()` (Generic Host console app) to `WebApplication.CreateBuilder()` (ASP.NET Core Web API) immediately, as part of feature 1.1.2 or a new task in feature 1.1.

**Transport mode is controlled by configuration:**

| `Transport:Mode` config value | Behavior |
|---|---|
| `http` (default) | ASP.NET Core Kestrel HTTP server. MCP served via HTTP/SSE transport. `/health`, `/metrics`, and any future HTTP endpoints available. |
| `stdio` | stdin/stdout transport. ASP.NET Core pipeline still runs but Kestrel is not started. Suitable for Claude Desktop integration via `mcp.json`. |

The `Program.cs` branching reads the config at startup and registers the appropriate MCP transport. All tool, resource, and prompt registrations are transport-agnostic and unchanged regardless of mode.

`mcp.json` continues to use `stdio` mode (launched as a local process with `Transport__Mode=stdio`). CI and E2E tests use `http` mode via `WebApplicationFactory<Program>`.

**`IFirmIdentityContext` abstraction:**

Because the Web API uses JWT Bearer auth (Entra ID) in production but tests need to inject synthetic identities, the authorization layer is written against an `IFirmIdentityContext` interface registered in DI. In production this is resolved from the JWT claims. In tests, a `FakeIdentityContext` is substituted via `WebApplicationFactory.WithWebHostBuilder`. This is specified in story 1.3.5 and is a prerequisite for all Epic 7 authorization tests.

## Consequences

**Easier:**
- E2E and integration tests use `WebApplicationFactory<Program>` — standard, fast, no process boundary
- `/health` and other supplemental HTTP endpoints are first-class
- Authentication middleware (`AddJwtBearer`) fits naturally in the ASP.NET Core pipeline
- No planned breaking change in Epic 6 for the transport migration; that work is done now
- Multi-client scenarios supported from the start

**Harder:**
- Slightly more boilerplate in `Program.cs` than the Generic Host (minimal, and the MCP SDK supports both)
- Claude Desktop `mcp.json` must pass `Transport__Mode=stdio` (or an environment-variable equivalent) to get stdio behavior; without it the server starts Kestrel and Claude Desktop cannot connect. This must be documented in the dev environment setup (story 1.1.3).
- The `stdio` mode still works but is now a secondary execution path, which means integration tests for the MCP protocol level should be run in `http` mode wherever possible.

**Open questions:**
- Should the HTTP transport use **SSE** (the MCP SDK's `WithHttpTransport`) or a minimal JSON-RPC-over-HTTP approach? The MCP SDK's HTTP transport handles this; verify the SDK version supports it before 1.1.2 is implemented.
- Does `WebApplicationFactory` support the MCP SDK's hosted service registration cleanly? Validate this as the first step of 7.1.3.
