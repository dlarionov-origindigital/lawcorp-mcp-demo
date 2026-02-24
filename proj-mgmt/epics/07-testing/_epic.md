# Epic 7: End-to-End Testing

**Status:** BACKLOG
**Goal:** Establish a layered, persona-driven test suite that validates the authorization model, all MCP tool handlers, and the Web API surface end-to-end, using swappable database and identity adapters so the test infrastructure can run in CI without external dependencies.

## Features

| ID | Feature | Status |
|---|---|---|
| [7.1](./7.1-test-infrastructure.md) | Test Infrastructure | BACKLOG |
| [7.2](./7.2-persona-fixture.md) | Persona Fixture | BACKLOG |
| [7.3](./7.3-authorization-tests.md) | Authorization Scenario Tests | BACKLOG |
| [7.4](./7.4-tool-e2e-tests.md) | Tool E2E Tests | BACKLOG |
| [7.5](./7.5-supplemental-e2e-tests.md) | Supplemental E2E Tests | BACKLOG |

## Dependencies

Depends on:
- [Epic 1](../01-foundation/_epic.md) — auth handler + `IFirmIdentityContext` abstraction (story 1.3.5)
- [Epic 2](../02-data-model/_epic.md) — EF Core DbContext + MockDataSeeder
- [Epic 3](../03-mcp-tools/_epic.md) — tool handlers (7.4 blocked until tools exist)

Blocks: Nothing (testing epic; gating deployment is a CI/CD concern)

## Relationship to Epic 6

Feature 6.3 (Testing) in Epic 6 describes test coverage at a high level. Epic 7 supersedes it with a fully specified, persona-driven approach. When Epic 7 is completed, mark 6.3 as `DONE` by reference.

See [ADR-004](../../decisions/004-dual-transport-web-api-primary.md) for the transport and identity abstraction decisions that make this testing approach possible.

## Test Tiers

| Tier | Runner | DB | Identity | Speed | CI |
|---|---|---|---|---|---|
| **Unit** | xUnit | None / mocked | Fake `ClaimsPrincipal` | < 1s each | Always |
| **Integration** | xUnit + `WebApplicationFactory` | SQLite in-memory | `FakeIdentityContext` | Seconds | Always |
| **E2E (real infra)** | xUnit `[Trait("Category","E2E")]` | LocalDB or Azure SQL | Real Entra token | Minutes | On-demand only |

CI gates on unit + integration. E2E tests against real infra are run manually or on a scheduled pipeline.

## Success Criteria

- [ ] `PersonaFixture` seeds a deterministic, minimal dataset with controlled relationships covering all six roles
- [ ] DB provider adapter allows switching between SQLite in-memory, LocalDB, and Azure SQL via configuration
- [ ] `IFirmIdentityContext` is injectable in tests with no real Entra ID token required
- [ ] Authorization tests cover every (role × entity × operation) cell in the PRD 4.2 permissions matrix
- [ ] Row-level security tests assert that a persona cannot retrieve entities outside their access scope
- [ ] Field redaction tests assert that the Intern persona receives redacted content where the PRD specifies
- [ ] Identity passthrough tests verify that each persona's identity produces correct access boundaries across both Graph and local DB resources ([ADR-005](../../decisions/005-oauth-identity-passthrough.md))
- [ ] Every MCP tool has at least one passing-access and one denied-access test using the appropriate persona
- [ ] Health check and transport configuration smoke tests pass in CI
- [ ] All unit + integration tests run in < 60 seconds in CI
