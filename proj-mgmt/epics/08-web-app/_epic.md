# Epic 8: Web Application

**Status:** BACKLOG
**Goal:** Build a white-labelled Blazor Web App that authenticates users via Entra ID, connects to the Law-Corp MCP server as an MCP client, and provides a browser-based surface for invoking tools, viewing authorization audit trails, and running Playwright E2E tests against real personas.

**ADR:** [ADR-006: Web app architecture](../../decisions/006-web-app-architecture.md)

## Features

| ID | Feature | Status |
|---|---|---|
| [8.1](./8.1-blazor-project-foundation.md) | Blazor Project Foundation | BACKLOG |
| [8.2](./8.2-mcp-client-integration.md) | MCP Client Integration | BACKLOG |
| [8.3](./8.3-auth-audit-observability.md) | Authorization Audit & Observability | BACKLOG |
| [8.4](./8.4-white-labelling.md) | White-Labelling & Theming | BACKLOG |
| [8.5](./8.5-playwright-e2e/8.5-playwright-e2e.md) | Playwright E2E Tests | BACKLOG |
| [8.6](./8.6-header-links/8.6-header-links.md) | Header Links | IN PROGRESS |

## Key Architecture Decisions

- [ADR-006](../../decisions/006-web-app-architecture.md) — Blazor Web App (.NET 9, Interactive Server) as the MCP client demo and E2E test harness
- [ADR-005](../../decisions/005-oauth-identity-passthrough.md) — OAuth identity passthrough drives the auth flow end-to-end

## Solution Structure

```
src/
  LawCorp.Mcp.Web/           ← Blazor Web App (this epic)
  LawCorp.Mcp.Tests.E2E/     ← Playwright E2E tests (feature 8.5)
```

Both new projects reference `LawCorp.Mcp.Core` for shared domain types.

## Dependencies

Depends on:
- [Epic 1](../01-foundation/_epic.md) — auth middleware, `IFirmIdentityContext`, Entra ID app registrations
- [Epic 3](../03-mcp-tools/_epic.md) — MCP tools to invoke (8.2 is progressively useful as tools are implemented)

Blocks: Nothing directly (but enables Playwright-based verification of all other epics)

## Success Criteria

- [ ] User can log into the Blazor web app via Entra ID OIDC redirect
- [ ] Authenticated user can invoke MCP tools through the UI; results are scoped to their identity
- [ ] The UI displays the resolved identity (name, role, practice group) and the MCP communication trace
- [ ] Playwright scripts can log in as each of the six personas and verify different access results
- [ ] The app is white-labelled with configurable branding (logo, name, colors)
- [ ] Both `LawCorp.Mcp.Web` and `LawCorp.Mcp.Server` can be hosted in-process via `WebApplicationFactory` for integration tests
- [ ] Future Angular and React companion apps can reuse the same Playwright test scripts (shared test contract)
