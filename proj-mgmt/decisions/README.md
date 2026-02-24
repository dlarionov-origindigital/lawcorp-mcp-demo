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
