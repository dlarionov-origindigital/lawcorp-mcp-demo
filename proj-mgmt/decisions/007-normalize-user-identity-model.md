# ADR-007: Normalize user identity into a shared User table (3NF compliance)

**Status:** Proposed
**Date:** 2026-02-24

## Context

The current data model stores person-level identity attributes (`FirstName`, `LastName`, `Email`, `EntraObjectId`) independently in four separate tables:

| Table | Purpose | Identity columns |
|---|---|---|
| `Attorneys` | Lawyers with bar numbers, roles, billing rates | FirstName, LastName, Email, EntraObjectId |
| `Paralegals` | Non-lawyer staff supporting attorneys | FirstName, LastName, Email, EntraObjectId |
| `LegalAssistants` | Administrative support assigned to one attorney | FirstName, LastName, Email, EntraObjectId |
| `Interns` | Temporary staff with school affiliation | FirstName, LastName, Email, EntraObjectId |

### What's wrong with this

**1. Third Normal Form (3NF) violation — transitive dependencies**

The identity columns (`FirstName`, `LastName`, `Email`, `EntraObjectId`) depend on the person, not on their role as attorney/paralegal/etc. In 3NF, every non-key attribute must depend on the key, the whole key, and nothing but the key. Currently, if a person changes their email, their old records (as an intern, say) retain stale data — or worse, must be updated in multiple places.

**2. No unified identity for auth resolution**

`UserContextResolutionMiddleware` currently queries only `db.Attorneys` to resolve an Entra Object ID to a user. Paralegals, LegalAssistants, and Interns cannot authenticate at all — their EntraObjectId is stored but never queried. A unified `Users` table would provide a single lookup point for all personnel.

**3. Career progression is destructive**

A law firm intern who passes the bar and becomes an associate must be deleted from `Interns` and re-created in `Attorneys`. Their historical data (case assignments during internship, supervised work product, audit trail) is either lost or requires complex migration logic. In reality, firms track the full career arc of their people.

**4. CaseAssignment/TimeEntry/Document are attorney-only**

Foreign keys like `CaseAssignment.AttorneyId`, `TimeEntry.AttorneyId`, and `Document.AuthorId` all point to the `Attorneys` table. A paralegal cannot be assigned to a case, log time, or be credited as a document author — which is unrealistic in any law firm.

**5. No single "who did this?" query**

Audit logs, activity streams, and conflict-of-interest checks need to answer "who touched this case?" across all personnel types. With four separate tables and no shared key, this requires four UNION queries — fragile and slow.

### Why the current model was chosen (and its merits)

The per-type-table approach (known as Table-Per-Concrete-Type / TPC in EF Core) was selected during initial prototyping for pragmatic reasons:

| Advantage | Explanation |
|---|---|
| **Simplicity of initial build** | Each entity maps 1:1 to a table with no JOINs for basic reads. A query for "all attorneys" doesn't need to JOIN to a base table. |
| **Type-specific schemas** | Each table carries only the columns it needs: `BarNumber` and `HourlyRate` on `Attorneys`, `School` on `Interns`, `AssignedAttorneyId` on `LegalAssistants`. No nullable columns for irrelevant fields. |
| **EF Core convention alignment** | EF Core's default behaviour maps each `DbSet<T>` to a table. No inheritance configuration, no discriminator columns, no TPH/TPT mapping needed. |
| **Seed data simplicity** | Mock data generators and persona seeders create typed objects directly without worrying about base/derived relationships. |
| **No premature abstraction** | The original scope was a demo/reference architecture focused on MCP capabilities, not HR management. The per-type approach was "good enough" for the tools-first iteration. |

These advantages are real but don't scale to the enterprise-grade requirements the project now targets: multi-persona auth, career progression, unified audit, and cross-role authorization.

## Decision

Introduce a `Users` table as the canonical identity record for all firm personnel. Role-specific attributes move to satellite tables (`AttorneyDetails`, `ParalegalDetails`, etc.) linked by a foreign key to `Users.Id`. A user's firm role is tracked with a `FirmRole` discriminator/enum on the `Users` table, and historical role assignments are preserved in a `UserRoleHistory` table.

### Target schema (conceptual)

```
Users
├── Id (PK)
├── FirstName
├── LastName
├── Email (unique)
├── EntraObjectId (unique, nullable)
├── FirmRole (enum: Partner, Associate, OfCounsel, Paralegal, LegalAssistant, Intern)
├── HireDate
├── IsActive
└── PracticeGroupId (FK, nullable)

AttorneyDetails
├── UserId (PK, FK → Users.Id)
├── BarNumber
├── HourlyRate
└── Role (Partner | Associate | OfCounsel) — mirrors Users.FirmRole for attorney-specific queries

InternDetails
├── UserId (PK, FK → Users.Id)
├── School
├── SupervisorId (FK → Users.Id)
├── StartDate
└── EndDate

UserRoleHistory
├── Id (PK)
├── UserId (FK → Users.Id)
├── FirmRole (previous)
├── EffectiveDate
├── EndDate (nullable — null means current)
└── Notes
```

- `LegalAssistant` and `Paralegal` don't need detail tables unless they acquire role-specific columns beyond what `Users` provides. Their assignment relationships (`AssignedAttorneyId`, `AssignedAttorneys`) move to a `UserAssignment` join table or stay as nullable FKs on `Users`.
- `CaseAssignment.AttorneyId`, `TimeEntry.AttorneyId`, `Document.AuthorId` all become `UserId` → `Users.Id`, enabling any personnel type to be assigned/credited.
- `IUserContext.UserId` and `IFirmIdentityContext.UserId` become the `Users.Id`, no longer specific to the `Attorneys` table.
- `UserContextResolutionMiddleware` queries a single `Users` table by `EntraObjectId` instead of four.

### Naming convention

The table is named `Users` (not `Employees` or `Personnel`) because it aligns with the identity/auth domain: these are the people who log in. The term is familiar to .NET developers (ASP.NET Identity uses `AspNetUsers`), requires no domain-specific explanation, and matches the `IUserContext` interface already in use.

### Migration strategy

This is a **breaking schema change** that touches nearly every table in the system. The recommended approach:

1. **Add `Users` table and populate** — create the table, INSERT from existing Attorney/Paralegal/LegalAssistant/Intern tables with deterministic ID mapping
2. **Add new FK columns** — `CaseAssignment.UserId`, `TimeEntry.UserId`, `Document.AuthorUserId`, etc.
3. **Backfill FK values** — copy from the old `AttorneyId` columns using the ID mapping
4. **Update EF Core model** — new `User` entity, update navigation properties, update `DbContext`
5. **Update auth resolution** — `UserContextResolutionMiddleware` queries `Users` instead of `Attorneys`
6. **Update tools and queries** — all tool code that queries by `AttorneyId` switches to `UserId`
7. **Update seeders** — `PersonaSeeder` creates `User` records first, then `AttorneyDetails`/`InternDetails`
8. **Drop old columns** — remove `AttorneyId` FKs, drop old tables (or rename as archive)

Given that the project uses `EnsureDeletedAsync` / `EnsureCreatedAsync` for dev seeding (no production migration history yet), the migration can be done as a clean schema redesign rather than an incremental ALTER TABLE migration.

## Consequences

### What becomes easier

- **Auth resolution** — single query against `Users` by `EntraObjectId`, works for all personnel types
- **Career progression** — promote an intern to associate by updating `Users.FirmRole` and adding an `AttorneyDetails` row; historical `InternDetails` preserved
- **Unified audit** — all `UserId` foreign keys reference one table; "who touched this case?" is a single JOIN
- **Authorization** — `IFirmIdentityContext` populated from one consistent source; no type-specific branching
- **Cross-role capabilities** — paralegals can log time, be assigned to cases, author documents
- **Reporting** — headcount, utilization, and billing queries don't require UNION across multiple tables

### What becomes harder

- **Schema complexity** — JOINs required to get role-specific attributes (e.g., `Users` JOIN `AttorneyDetails` for `BarNumber`)
- **Seed data** — generators must create `User` + optional detail records instead of flat objects
- **EF Core configuration** — requires explicit inheritance mapping (TPT or owned entities), not just convention
- **Blast radius** — touches Models, Data, Auth, MockData, Server/Tools, and all downstream queries

### Open questions

1. **TPH vs. TPT vs. owned entities** — Which EF Core inheritance strategy for `User` → `AttorneyDetails`? TPH (single table with discriminator) is simplest but adds nullable columns. TPT (table-per-type) matches the proposed schema but has JOIN overhead. Needs benchmarking.
2. **`FirmRole` enum scope** — Should it cover all six current types, or use a more generic taxonomy? Consider future roles (e.g., External Counsel, Contract Attorney).
3. **Backward compatibility** — Should old table names (`Attorneys`, `Paralegals`) be retained as views for read compatibility during transition?
4. **ID stability** — The persona seeder relies on deterministic IDs (Harvey=1, Kim=2, Alan=3). The migration must preserve these or update `persona-seed.json` accordingly.

## References

- [Database Normalization — Third Normal Form](https://en.wikipedia.org/wiki/Third_normal_form)
- [EF Core Inheritance Mapping](https://learn.microsoft.com/en-us/ef/core/modeling/inheritance)
- [Martin Fowler — Class Table Inheritance](https://martinfowler.com/eaaCatalog/classTableInheritance.html)
- ADR-002: Use EF Core as the ORM with SQL Server
- ADR-005: OAuth identity passthrough (depends on user resolution)
