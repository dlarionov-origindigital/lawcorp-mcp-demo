# Epic 2: Data Model & Mock Data Generation

**Status:** IN PROGRESS
**Goal:** Define all EF Core entities, configure the database schema, build the mock data generator, and seed a realistic Law-Corp database.

## Features

| ID | Feature | Status |
|---|---|---|
| [2.1](./2.1-people-org-entities.md) | People & Organization Entities | DONE |
| [2.2](./2.2-case-management-entities.md) | Case Management Entities | DONE |
| [2.3](./2.3-document-entity.md) | Document Entity | DONE |
| [2.4](./2.4-billing-entities.md) | Billing Entities | DONE |
| [2.5](./2.5-calendar-deadline-entities.md) | Calendar & Deadline Entities | DONE |
| [2.6](./2.6-research-intake-audit-entities.md) | Research, Intake & Audit Entities | DONE |
| [2.7](./2.7-database-configuration.md) | Database Configuration | IN PROGRESS |
| [2.8](./2.8-mock-data-generator.md) | Mock Data Generator | IN PROGRESS |

## Dependencies

Depends on: Epic 1 (Foundation)
Blocks: Epic 3, Epic 4, Epic 5

## Success Criteria

- [x] All 22 entity model files exist in LawCorp.Mcp.Core/Models/
- [x] LawCorpDbContext registers all entities
- [ ] `dotnet ef database update` creates the full schema with all indexes
- [x] Mock data generator framework is in place
- [x] Entity generators produce realistic data
- [ ] Medium profile seeds ~80 attorneys, ~150 cases in < 30 seconds
