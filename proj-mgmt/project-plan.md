# Law-Corp MCP Server — Project Plan

> Derived from [PRD v0.1.0-draft](./prd.md) on 2026-02-23

---

## Project Summary

Build an enterprise-grade MCP (Model Context Protocol) server as a .NET Web API that exposes Law-Corp LLP's internal systems — case management, document management, billing, research, and intake — as MCP tools, resources, and prompts. The server demonstrates real-world MCP patterns for a regulated industry with Entra ID authentication and role-based authorization.

---

## Work Breakdown Structure

| # | Epic | Features | Tasks | Total Cards |
|---|---|---|---|---|
| 1 | [Foundation & Infrastructure](./epics/01-foundation-infrastructure.md) | 3 | 7 | 10 |
| 2 | [Data Model & Mock Data](./epics/02-data-model-mock-data.md) | 8 | 24 | 32 |
| 3 | [MCP Tools](./epics/03-mcp-tools.md) | 7 | 28 | 28 |
| 4 | [MCP Resources](./epics/04-mcp-resources.md) | 3 | 7 | 7 |
| 5 | [MCP Prompts & Sampling](./epics/05-mcp-prompts-sampling.md) | 5 | 15 | 15 |
| 6 | [Protocol Features & Deployment](./epics/06-protocol-deployment.md) | 4 | 12 | 12 |
| | **Totals** | **30** | **93** | **104** |

---

## Epic Dependency Graph

```
Epic 1: Foundation & Infrastructure
  ├─► Epic 2: Data Model & Mock Data
  │     ├─► Epic 3: MCP Tools  ──────────┐
  │     ├─► Epic 4: MCP Resources         ├─► Epic 6: Protocol Features & Deployment
  │     └─► Epic 5: MCP Prompts & Sampling┘
  └─► Epic 6: Protocol Features (cross-cutting, iterative)
```

**Critical path:** Epic 1 → Epic 2 → Epic 3 (tools are the bulk of the work)

---

## Suggested Implementation Order

### Phase 1: Foundation (Epics 1 + 2)

Build the skeleton that everything depends on.

1. **Solution structure** — Create .NET solution, project layout, build configuration
2. **Entity models** — All EF Core entities, DbContext, initial migration
3. **Mock data generator** — Build generator, seed the database
4. **Authentication** — Entra ID token validation, OBO flow
5. **Authorization layer** — Role-based handlers, row-level filters, field-level redaction, audit log

**Exit criteria:** Database seeded with realistic data, auth pipeline works end-to-end, role-based queries return correctly filtered results.

### Phase 2: Core Tools (Epic 3, features 3.1–3.3)

Implement the most-used tool categories first.

6. **Case Management tools** — search, get, update status, assign, timeline, add note
7. **Document Management tools** — search, get, draft, update status, list by case
8. **Client & Contact tools** — search, get, conflict check, contacts

**Exit criteria:** Core case/document/client workflows work through MCP tool calls with authorization.

### Phase 3: Supporting Tools (Epic 3, features 3.4–3.7)

Build out the remaining tool domains.

9. **Billing & Time Entry tools** — log time, search, billing summary, invoices
10. **Calendar & Deadline tools** — hearings, deadlines, add event, conflicts
11. **Legal Research tools** — precedents, statutes, research memos
12. **Intake & Onboarding tools** — create request, conflict check, approve, engagement letter

**Exit criteria:** All 35+ tools implemented and tested with authorization.

### Phase 4: Resources & Prompts (Epics 4 + 5)

Layer on the read-only resources and prompt templates.

13. **Static resources** — firm profile, directories, reference data
14. **Dynamic resources** — case, client, attorney, calendar URI templates
15. **Subscription resources** — deadline, case update, assignment, billing notifications
16. **Prompt templates** — All 12 prompts with registry and discovery
17. **Sampling** — Document classification, deadline extraction, conflict enhancement, time entry enhancement

**Exit criteria:** Full MCP capability surface (tools + resources + prompts + sampling) working.

### Phase 5: Polish & Deploy (Epic 6)

Cross-cutting protocol features and production deployment.

18. **Protocol features** — Roots, logging, pagination, progress, cancellation, error handling
19. **Testing** — Unit tests (auth), integration tests (tools, resources, prompts)
20. **Deployment** — Azure Foundry configuration, CI/CD, documentation

**Exit criteria:** Solution deployed to Azure Foundry, all tests passing, documentation complete.

---

## imdone Board Usage

All work items live in the `proj-mgmt/epics/` markdown files as imdone cards. The board is configured with these lists:

| List | Purpose |
|---|---|
| **BACKLOG** | All cards start here — groomed and ready for prioritization |
| **TODO** | Committed to current sprint/iteration |
| **DOING** | Actively being worked on |
| **REVIEW** | Implementation complete, awaiting review |
| **DONE** | Reviewed and accepted |

### Card Conventions

- Cards are tagged with `+epic`, `+feature`, `+story`, or `+task` for hierarchy
- Domain tags: `+foundation`, `+auth`, `+data-model`, `+mock-data`, `+case-mgmt`, `+doc-mgmt`, `+client-mgmt`, `+billing`, `+calendar`, `+research`, `+intake`, `+resources`, `+prompts`, `+sampling`, `+protocol`, `+deployment`
- Card ordering (the number after the list name) groups cards by epic and feature
- Move cards between lists by changing the list prefix (e.g., `BACKLOG:10` → `TODO:10` → `DOING:10`)

### Filtering by Epic

Use imdone's tag filter to view cards for a specific epic:
- `+foundation +auth` — Epic 1
- `+data-model +mock-data` — Epic 2
- `+case-mgmt +doc-mgmt +client-mgmt +billing +calendar +research +intake` — Epic 3
- `+resources` — Epic 4
- `+prompts +sampling` — Epic 5
- `+protocol +deployment` — Epic 6

---

## Open Questions (from PRD)

These should be resolved before moving cards out of BACKLOG:

- [ ] Single-office or multi-office model?
- [ ] Separate "matter" concept vs. "case"?
- [ ] Mock data generator as separate CLI or EF migration seed?
- [ ] Document content depth — metadata only or full legal-flavored text?
- [ ] Model external integration stubs (e-filing, Westlaw)?

---

## Success Criteria (from PRD)

- [ ] All tools enforce authorization via custom layer + Entra ID claims
- [ ] Mock data is generated deterministically and seeds SQL Express
- [ ] Resources correctly resolve URI templates with read-level authorization
- [ ] Prompts produce well-structured, domain-appropriate output
- [ ] Sampling use cases demonstrably enhance tool capabilities
- [ ] Audit log captures every data access with full identity context
- [ ] Solution compiles, runs locally with SQL Express, and deploys to Azure Foundry
