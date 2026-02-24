# Epic 3: MCP Tools

**Status:** BACKLOG
**Goal:** Implement all 35+ MCP tools across 7 functional domains, each with role-based authorization enforced per the permissions matrix.

## Features

| ID | Feature | Status |
|---|---|---|
| [3.1](./3.1-case-management-tools.md) | Case Management Tools | BACKLOG |
| [3.2](./3.2-document-tools.md) | Document Management Tools | BACKLOG |
| [3.3](./3.3-client-contact-tools.md) | Client & Contact Management Tools | BACKLOG |
| [3.4](./3.4-billing-tools.md) | Billing & Time Entry Tools | BACKLOG |
| [3.5](./3.5-calendar-tools.md) | Court Calendar & Deadlines Tools | BACKLOG |
| [3.6](./3.6-research-tools.md) | Legal Research Tools | BACKLOG |
| [3.7](./3.7-intake-tools.md) | Intake & Onboarding Tools | BACKLOG |

## Dependencies

Depends on: Epic 1 (Foundation — auth + authz), Epic 2 (Data Model)
Blocks: Epic 6 (testing covers tool handlers)

## Success Criteria

- [ ] All 35+ tools are implemented and registered
- [ ] Role-based authorization is enforced on every tool call
- [ ] Cursor-based pagination works on all search/list tools
- [ ] Progress reporting works on long-running tools
- [ ] All tools are covered by integration tests (Epic 6)
