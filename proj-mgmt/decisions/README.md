# Architecture Decision Records

An ADR captures the reasoning behind a significant architectural or product decision. Its value is not the decision itself — that's visible in the code — but the **context and trade-offs** that explain why.

---

## Format

```markdown
# ADR-NNN: Short title in imperative form

**Status:** Proposed | Accepted | Deprecated | Superseded by ADR-NNN
**Date:** YYYY-MM-DD

## Context

What situation, constraint, or question required a decision?
What alternatives were available?

## Decision

What was decided, stated directly.

## Consequences

What becomes easier as a result?
What becomes harder?
What new constraints does this introduce?
What open questions remain?
```

---

## Index

| ADR | Title | Status |
|---|---|---|
| [001](./001-stdio-transport.md) | Use stdio transport for initial MCP server | Superseded by ADR-004 |
| [002](./002-ef-core-orm.md) | Use EF Core as the ORM with SQL Server | Accepted |
| [003](./003-single-project-host.md) | Host MCP server as a single-process console app | Superseded by ADR-004 |
| [004](./004-dual-transport-web-api-primary.md) | Adopt ASP.NET Core Web API as primary host; retain stdio as a configuration mode | Accepted |
| [005](./005-oauth-identity-passthrough.md) | Use OAuth identity passthrough as the user-delegated access pattern | Accepted |
| [006](./006-web-app-architecture.md) | Add a white-labelled Blazor Web App as the MCP client demo and E2E test harness | Accepted |
| [007](./007-normalize-user-identity-model.md) | Normalize user identity into a shared User table (3NF compliance) | Accepted |
| [008](./008-cqrs-dispatch-pattern.md) | CQRS dispatch pattern for MCP tool handlers | Proposed |
| [009](./009-swagger-ui-local-dev.md) | Add interactive Swagger UI for local development ergonomics | Accepted |
