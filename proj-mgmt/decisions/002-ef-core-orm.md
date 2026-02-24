# ADR-002: Use EF Core as the ORM with SQL Server

**Status:** Accepted
**Date:** 2026-02-23

## Context

The server requires a relational data store for the Law-Corp domain model (22 entities, complex relationships). Options considered:

- **EF Core + SQL Server Express** — Microsoft's flagship ORM for .NET with a free local server edition
- **Dapper + SQL Server** — lightweight micro-ORM, more control over SQL
- **EF Core + SQLite** — simpler local setup, no installation required
- **In-memory only** — mock data held in static lists, no persistence

The PRD specifies:
- Row-level security enforced at the application layer (not the database)
- Audit logging for all data access
- Deterministic mock data seeding
- Eventual deployment to Azure (where Azure SQL is the natural fit)

## Decision

Use **EF Core 9** with the **SQL Server provider**, targeting a **local SQL Express** instance for development and **Azure SQL** for production.

Row-level filtering is implemented via **EF Core global query filters**, which are applied transparently to all queries scoped to the current user context. This is a first-class EF Core feature that avoids duplicating filter logic in every repository method.

## Consequences

**Easier:**
- Global query filters make row-level security declarative and hard to accidentally bypass
- EF Core migrations provide a reproducible schema evolution story
- `LawCorpDbContext` is the single place to configure all entity relationships and indexes
- Swapping to Azure SQL in production is a connection string change only

**Harder:**
- SQL Server Express requires local installation (Docker Compose is an option to remove this friction — see Epic 1 BACKLOG:30)
- EF Core global query filters add a dependency on `IHttpContextAccessor` or a scoped user context service being correctly registered — this must be handled carefully at DI setup time
- N+1 query risks are real at scale; query patterns should be reviewed against the index plan (BACKLOG:180)

**Open questions:**
- Should the mock data seeder run as part of EF Core migrations or as a separate `dotnet run --seed` command? Keeping it separate is safer for production (ADR to be written once decided — see PRD open question).
