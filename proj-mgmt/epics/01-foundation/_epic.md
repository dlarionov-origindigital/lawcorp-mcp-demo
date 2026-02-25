# Epic 1: Project Foundation & Infrastructure

**Status:** IN PROGRESS
**Goal:** Stand up the .NET solution structure, MCP server skeleton, authentication with Microsoft Entra ID, the custom authorization layer, OAuth identity passthrough for user-delegated access to all downstream resources ([ADR-005](../../decisions/005-oauth-identity-passthrough.md)), an external downstream API for multi-hop OBO demonstration, and a CQRS dispatch layer for MCP tool handlers ([ADR-008](../../decisions/008-cqrs-dispatch-pattern.md)).

## Features

| ID | Feature | Status |
|---|---|---|
| [1.1](./1.1-solution-structure/1.1-solution-structure.md) | Solution & Project Structure | DONE |
| [1.2](./1.2-authn/1.2-authentication.md) | Authentication — Microsoft Entra ID | IN PROGRESS |
| [1.3](./1.3-authz/1.3-authorization.md) | Custom Authorization Layer | BACKLOG |
| [1.4](./1.4-external-api/1.4-external-api.md) | External Downstream API Service | BACKLOG |
| [1.5](./1.5-cqrs-dispatch/1.5-cqrs-dispatch.md) | CQRS Dispatch Pattern for MCP Tools | BACKLOG |

## Key Architecture Decisions

- [ADR-004](../../decisions/004-dual-transport-web-api-primary.md) — ASP.NET Core Web API as primary host
- [ADR-005](../../decisions/005-oauth-identity-passthrough.md) — OAuth identity passthrough as user-delegated access pattern
- [ADR-008](../../decisions/008-cqrs-dispatch-pattern.md) — CQRS dispatch pattern for MCP tool handlers

## Dependencies

Depends on: None
Blocks: Epic 2, Epic 3, Epic 4, Epic 5, Epic 6

## Success Criteria

- [x] Solution builds cleanly with all projects referenced
- [x] MCP server skeleton responds to protocol messages
- [ ] Entra ID tokens are validated end-to-end
- [ ] Role-based authorization enforced on all tool calls
- [ ] Row-level security filters data by user role
- [ ] Audit log captures all data access
- [ ] User identity flows through to Microsoft Graph via OBO token exchange ([ADR-005](../../decisions/005-oauth-identity-passthrough.md))
- [ ] User identity flows through to external downstream API via multi-hop OBO
- [ ] User identity drives local database access scoping via `IFirmIdentityContext`
- [ ] Different personas (roles) produce different access results across all downstream resources
- [ ] MCP tools dispatch via MediatR; handlers determine data source ([ADR-008](../../decisions/008-cqrs-dispatch-pattern.md))
- [ ] Three Entra ID app registrations documented and tested (MCP Server, Web App, External API)
