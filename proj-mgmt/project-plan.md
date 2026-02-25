# Law-Corp MCP Server — Project Plan

> Derived from [PRD v0.1.0-draft](./prd.md) on 2026-02-23

---

## Project Summary

Build an enterprise-grade MCP (Model Context Protocol) server as a .NET Web API that exposes Law-Corp LLP's internal systems — case management, document management, billing, research, and intake — as MCP tools, resources, and prompts. The server demonstrates real-world MCP patterns for a regulated industry with Entra ID authentication and role-based authorization.

---

## Work Breakdown Structure

| # | Epic | Features | Tasks | Total Cards |
|---|---|---|---|---|
| 1 | [Foundation & Infrastructure](./epics/01-foundation/_epic.md) | 3 | 13 | 16 |
| 2 | [Data Model & Mock Data](./epics/02-data-model/_epic.md) | 8 | 24 | 32 |
| 3 | [MCP Tools](./epics/03-mcp-tools/_epic.md) | 7 | 28 | 28 |
| 4 | [MCP Resources](./epics/04-mcp-resources/_epic.md) | 3 | 7 | 7 |
| 5 | [MCP Prompts & Sampling](./epics/05-mcp-prompts-sampling/_epic.md) | 5 | 15 | 15 |
| 6 | [Protocol Features & Deployment](./epics/06-protocol-deployment/_epic.md) | 4 | 13 | 13 |
| 7 | [End-to-End Testing](./epics/07-testing/_epic.md) | 5 | 16 | 21 |
| 8 | [Web Application](./epics/08-web-app/_epic.md) | 5 | 18 | 23 |
| | **Totals** | **40** | **134** | **155** |

---

## Epic Dependency Graph

```
Epic 1: Foundation & Infrastructure  (includes IFirmIdentityContext — 1.3.5)
  ├─► Epic 2: Data Model & Mock Data
  │     ├─► Epic 3: MCP Tools  ──────────┐
  │     ├─► Epic 4: MCP Resources         ├─► Epic 6: Protocol Features & Deployment
  │     └─► Epic 5: MCP Prompts & Sampling┘
  └─► Epic 6: Protocol Features (cross-cutting, iterative)
  │
  ├─► Epic 7: End-to-End Testing (7.1 + 7.2 + 7.3 can start after Epic 1)
  │     └─► (7.4 Tool E2E blocked until Epic 3 tools exist)
  │
  ├─► Epic 8: Web Application (8.1 can start after Epic 1; 8.2+ progressively useful as Epics 3–5 deliver)
        ├─► 8.1 Blazor foundation (depends on Epic 1 auth)
        ├─► 8.2 MCP client integration (progressively useful as Epic 3 tools are built)
        ├─► 8.3 Auth audit UI (depends on 8.2 + 1.3.4 audit logging)
        ├─► 8.4 White-labelling (can start in parallel with 8.2)
        └─► 8.5 Playwright E2E (depends on 8.1–8.3 + 7.2 persona fixture)
```

**Critical path:** Epic 1 → Epic 2 → Epic 3 (tools are the bulk of the work)

**Testing path:** Epic 7 features 7.1–7.3 can begin in parallel with Epic 2. Feature 7.4 (Tool E2E) is blocked until Epic 3 tools are implemented.

**Web app path:** Epic 8 feature 8.1 (Blazor foundation) can start as soon as Epic 1 auth is complete. Features 8.2–8.3 become progressively more useful as Epic 3 tools are implemented. Feature 8.5 (Playwright E2E) depends on the full auth audit UI and the persona fixture from Epic 7.

**ADR notes:**
- The host has been migrated from Generic Host console app to ASP.NET Core Web API (see [ADR-004](./decisions/004-dual-transport-web-api-primary.md)). stdio remains available via `Transport:Mode=stdio` configuration. This enables `WebApplicationFactory`-based E2E tests in Epic 7 and eliminates the planned host migration in Epic 6.
- OAuth identity passthrough is the canonical user-delegated access pattern (see [ADR-005](./decisions/005-oauth-identity-passthrough.md)). Every MCP tool call executes under the calling user's Entra ID identity. Downstream Graph resources (SharePoint, Calendar, Mail) are accessed via OBO token exchange; local database access is scoped by `IFirmIdentityContext` claim extraction. This is a core value proposition: "the MCP server can only do what the logged-in user can do."
- A Blazor Web App (.NET 9, Interactive Server) serves as the MCP client demo and Playwright E2E test harness (see [ADR-006](./decisions/006-web-app-architecture.md)). Angular and React companion apps are planned for future phases to provide framework comparison data.

---

## Suggested Implementation Order

### Phase 1: Foundation (Epics 1 + 2)

Build the skeleton that everything depends on.

1. **Solution structure** — Create .NET solution, project layout, build configuration
2. **Entity models** — All EF Core entities, DbContext, initial migration
3. **Mock data generator** — Build generator, seed the database
4. **Authentication** — Entra ID token validation, OBO flow
5. **Authorization layer** — Role-based handlers, row-level filters, field-level redaction, audit log
6. **Identity passthrough** — Graph client with OBO provider, downstream resource access (SharePoint, Calendar), consent flow handling, database identity-scoped access ([ADR-005](./decisions/005-oauth-identity-passthrough.md))

**Exit criteria:** Database seeded with realistic data, auth pipeline works end-to-end, role-based queries return correctly filtered results, user identity flows through to Graph and local DB.

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

### Phase 6: Web Application & Browser E2E (Epic 8)

Build the browser-based demo and test surface.

21. **Blazor foundation** — Create project, OIDC auth, Fluent UI shell, app registration
22. **MCP client integration** — HttpClientTransport with token passthrough, tool/resource/prompt UI
23. **Auth audit UI** — Identity panel, MCP trace viewer, authorization decision log
24. **White-labelling** — Design tokens, branding configuration
25. **Playwright E2E** — Login automation per persona, access control tests, full-stack audit verification

**Exit criteria:** Users can log in via Entra ID, invoke MCP tools through the browser, and see persona-scoped results. Playwright scripts verify identity passthrough across all six personas.

**Future:** Angular and React companion apps (separate epics, not yet planned) will reuse the same Playwright test scripts to provide framework comparison data. See [ADR-006](./decisions/006-web-app-architecture.md).

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
- `+web-app +mcp-client +e2e` — Epic 8

---

## Open Questions (from PRD)

These should be resolved before moving cards out of BACKLOG:

- [ ] Single-office or multi-office model?
- [ ] Separate "matter" concept vs. "case"?
- [ ] Mock data generator as separate CLI or EF migration seed?
- [ ] Document content depth — metadata only or full legal-flavored text?
- [ ] Model external integration stubs (e-filing, Westlaw)?

---

## Success Criteria (from PRD + ADR-006)

- [ ] All tools enforce authorization via custom layer + Entra ID claims
- [ ] Mock data is generated deterministically and seeds SQL Express
- [ ] Resources correctly resolve URI templates with read-level authorization
- [ ] Prompts produce well-structured, domain-appropriate output
- [ ] Sampling use cases demonstrably enhance tool capabilities
- [ ] Audit log captures every data access with full identity context
- [ ] Solution compiles, runs locally with SQL Express, and deploys to Azure Foundry
- [ ] Blazor web app authenticates via Entra ID and invokes MCP tools with identity passthrough
- [ ] Playwright E2E tests verify persona-scoped access across all six roles
