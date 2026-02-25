# ADR-005: Use OAuth identity passthrough as the user-delegated access pattern

**Status:** Accepted
**Date:** 2026-02-24

## Context

The Law-Corp MCP server must demonstrate enterprise-grade authorization where every action is scoped to the authenticated user's identity. The core value proposition is: **"the MCP server should only be able to do what the logged-in user can do."**

Three categories of downstream resources require user-delegated access:

1. **Microsoft Graph APIs** — SharePoint documents, Outlook calendar events, mail, and other M365 data that belong to the user
2. **Local SQL Express database** — case management data filtered by the user's role, practice group, and case assignments via the custom authorization layer (Feature 1.3)
3. **Future external services** — any API that accepts delegated user tokens

Microsoft's Azure AI Foundry documentation identifies five authentication methods for MCP server connections, only one of which preserves the calling user's identity:

| Method | User Context Persists |
|---|---|
| Key-based (API key / PAT) | No |
| Entra auth (agent identity) | No |
| Entra auth (project managed identity) | No |
| **OAuth identity passthrough** | **Yes** |
| Unauthenticated | No |

For the project's stated requirement — "act as the user" for calendar, mail, documents, database, and all other resources — **OAuth identity passthrough** is the only viable pattern.

**Alternatives considered:**

- **Shared service account / API key** — A single identity for all users. Simpler to implement, but loses per-user scoping entirely. Every user sees every user's data. Rejected: violates the core value proposition.
- **Agent-managed identity** — The MCP server acts as itself, not as the user. Useful for system-level operations but cannot satisfy "calendar as them, mail as them." Rejected for user-facing tools.
- **Custom session-based auth** — The MCP server manages its own user sessions and tokens outside the Foundry/Entra pipeline. Adds complexity, duplicates what Entra already provides, and creates a separate security surface. Rejected.

## Decision

Adopt **OAuth identity passthrough** as the canonical authentication pattern for all user-facing MCP tool calls. This means:

1. **Inbound:** Every MCP request carries an `Authorization: Bearer <token>` header containing an Entra ID access token that represents the calling user.

2. **Token validation:** The ASP.NET Core JWT Bearer middleware (story 1.2.1) validates the token's issuer, audience, and signature.

3. **Identity resolution:** `IFirmIdentityContext` (story 1.3.5) maps the validated JWT claims to a typed, domain-aware identity object providing `UserId`, `AttorneyRole`, `PracticeGroupId`, `AssignedCaseIds`, and `AssignedAttorneyId`.

4. **Downstream access — Microsoft Graph (OBO):** When a tool needs to access Graph resources (SharePoint, Outlook Calendar, Mail) on behalf of the user, the server performs an On-Behalf-Of (OBO) token exchange (story 1.2.2) to obtain a Graph-scoped access token. The Graph call executes under the user's identity and consent.

5. **Downstream access — Local database:** The custom authorization layer (Feature 1.3) uses `IFirmIdentityContext` to enforce role-based access, row-level security, and field-level redaction. No OBO exchange is needed — the identity extracted from the inbound token drives all filtering.

6. **Consent flow:** When Foundry's OAuth passthrough encounters a tool requiring user consent for the first time, it returns an `oauth_consent_request` with a `consent_link`. The client surfaces this to the user. After consent, subsequent calls proceed without re-prompting (unless refresh tokens expire).

7. **Testability:** The `FakeIdentityContext` test double (Epic 7, task 7.1.2) allows all authorization tests to run without real Entra tokens. The persona fixture (Feature 7.2) provides six canonical personas with varying roles and access scopes, enabling systematic verification that identity passthrough enforces the correct boundaries.

### Two access patterns coexist

| Access Target | Token Flow | Authorization Layer |
|---|---|---|
| Microsoft Graph (SharePoint, Calendar, Mail) | Inbound user token → OBO exchange → Graph-scoped token | Graph API's own permissions + Entra consent |
| Local SQL Express database | Inbound user token → claim extraction → `IFirmIdentityContext` | Custom: role-based handlers, EF Core row-level filters, field-level redaction |

Both patterns start from the same inbound user token. The difference is whether the server needs to exchange the token (Graph) or simply read its claims (local DB).

## Consequences

**Easier:**
- Every tool call is scoped to the calling user's identity — "act as the user" is the default, not an opt-in
- The persona fixture (Feature 7.2) and `FakeIdentityContext` (7.1.2) enable comprehensive testing of access boundaries without real Entra tokens
- Azure Foundry's OAuth passthrough handles token lifecycle (consent, refresh, revocation) — the MCP server does not manage user sessions
- The same identity model works for both Graph-accessed resources and locally-stored data
- Personas with different roles and practice groups produce deterministic, testable access boundaries across both downstream patterns

**Harder:**
- Every downstream Graph call requires OBO token exchange, adding latency; token caching (story 1.2.2) mitigates this
- First-time consent requires user interaction (consent link flow); the client app must handle this UX
- App registrations become more complex: the MCP server app registration must declare API permissions for Graph scopes, and the Foundry connection must be configured for OAuth passthrough (not shared auth)
- Local development without Foundry requires a mechanism to inject test tokens or use the `FakeIdentityContext` pattern
- Testing Graph-calling tools in CI requires either a mock Graph endpoint or a dedicated test tenant

**Open questions:**
- Which specific Graph scopes are required? This depends on which downstream tools we implement (SharePoint read, Calendar read/write, Mail read, etc.). Scopes should follow least-privilege and be documented per tool in story 1.2.4.
- Should the MCP server support both Pattern A (direct user token to Graph) and Pattern B (OBO exchange), or standardize on OBO? Standardizing on OBO is recommended — it is the standard "API calls downstream API as user" approach in Entra.
- How should the consent flow be tested in CI? The `FakeIdentityContext` bypasses consent for local DB access, but Graph-calling tools may need a mock Graph endpoint or a dedicated test tenant with pre-consented permissions.

## References

- [Azure AI Foundry: MCP Authentication](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/mcp-authentication?view=foundry) — see [local analysis](../../docs/auth/passthrough.md)
- [ADR-004: Dual transport Web API](./004-dual-transport-web-api-primary.md)
- [Story 1.2.1: Entra ID auth middleware](../epics/01-foundation/1.2.1-entra-id-auth-middleware.md)
- [Story 1.2.2: OBO token exchange](../epics/01-foundation/1.2.2-obo-token-exchange.md)
- [Story 1.2.4: Downstream resource access](../epics/01-foundation/1.2.4-downstream-resource-access/1.2.4-downstream-resource-access.md)
- [Story 1.3.5: IFirmIdentityContext](../epics/01-foundation/1.3.5-firm-identity-context.md)
- [Feature 7.2: Persona Fixture](../epics/07-testing/7.2-persona-fixture.md)
