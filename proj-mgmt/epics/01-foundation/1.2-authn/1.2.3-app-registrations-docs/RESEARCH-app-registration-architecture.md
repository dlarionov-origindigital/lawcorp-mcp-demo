# RESEARCH: App Registration Architecture for Multi-Tier OBO MCP Server

**Date:** 2025-02-25
**Triggered by:** [BUG 1.2.3.1](./bugs/1.2.3.1-app-registration-architecture-mismatch.md) — current auth config describes two app registrations but lacks a custom downstream API to demonstrate true multi-hop OBO
**Story:** [1.2.3: Set up Entra ID app registrations documentation](./1.2.3-app-registrations-docs.md)
**ADR:** [ADR-005: OAuth identity passthrough](../../../decisions/005-oauth-identity-passthrough.md)

---

## Table of Contents

- [Problem Statement](#problem-statement)
- [Research Goals](#research-goals)
- [1. MCP Specification Auth Model](#1-mcp-specification-auth-model)
- [2. Entra ID Multi-Hop OBO Architecture](#2-entra-id-multi-hop-obo-architecture)
- [3. Proposed App Registration Architecture](#3-proposed-app-registration-architecture)
- [4. Solution Structure Refactoring](#4-solution-structure-refactoring)
- [5. CQRS / MediatR Dispatch Pattern](#5-cqrs--mediatr-dispatch-pattern)
- [6. External API Service Design](#6-external-api-service-design)
- [7. Proposed Project Management Changes](#7-proposed-project-management-changes)
- [8. Implementation Phases](#8-implementation-phases)
- [9. Open Questions](#9-open-questions)
- [10. Authoritative References](#10-authoritative-references)

---

## Problem Statement

The Law-Corp MCP Server's value proposition is **"the MCP server should only be able to do what the logged-in user can do"** ([ADR-005](../../../decisions/005-oauth-identity-passthrough.md)). Currently, the OBO token exchange is only demonstrated against Microsoft Graph — a first-party Microsoft service. The solution never proves that user identity flows through to a **custom downstream API** that we control and can inspect.

Enterprise customers adopting MCP servers will use them as **facades** over their own APIs, databases, and services. This reference architecture must demonstrate that pattern end-to-end: **User → MCP Client (Web App) → MCP Server → Custom Downstream API**, where each hop performs proper token exchange and the downstream API enforces its own authorization using the delegated user identity.

Additionally, the MCP server's tools currently access data through direct EF Core `DbContext` injection. If some data originates from an external API, tools need a dispatch abstraction so they don't couple to the data source implementation.

---

## Research Goals

1. Understand the MCP specification's three-party auth model and how it maps to Entra ID
2. Define the correct number and configuration of Entra ID app registrations
3. Design the solution structure to isolate the MCP server, MCP client, and external API
4. Evaluate CQRS/MediatR as a dispatch pattern for MCP tools
5. Propose project management changes (new/updated epics, features, stories)
6. Link all recommendations to authoritative sources

---

## 1. MCP Specification Auth Model

The MCP specification (2025-03-26 revision) defines a **three-party authorization architecture**:

| Party | Role | Our Mapping |
|---|---|---|
| **Client** | Initiates MCP requests, manages user auth | Blazor Web App (`LawCorp.Mcp.Web`) |
| **Resource Provider** | Exposes MCP tools/resources behind authorization | MCP Server (`LawCorp.Mcp.Server`) |
| **Authorization Server** | Issues and validates tokens | Microsoft Entra ID |

**Key protocol features:**

- **Protected Resource Metadata (RFC 9728):** The MCP server publishes a `/.well-known/oauth-protected-resource` document pointing clients to the authorization server. This enables dynamic discovery — clients don't need hardcoded auth endpoints.
- **Authorization Server Metadata (RFC 8414):** Entra ID's `/.well-known/openid-configuration` endpoint serves this role.
- **OAuth 2.1 with PKCE:** Mandatory for all HTTP-based MCP transports. The client uses Authorization Code + PKCE to obtain tokens. Entra ID supports this natively.
- **Resource Indicators (RFC 8707):** Tokens are scoped to a specific resource (audience), preventing cross-server token misuse. Entra ID's `api://` scheme implements this.

**Mapping to our architecture:** Entra ID is the authorization server. The Blazor web app is the MCP client. The MCP server is the resource provider. This three-party model is already implicit in our current auth setup, but making it explicit helps us understand where the external downstream API fits — it is a **fourth party**: a separate protected resource that the MCP server accesses on behalf of the user via OBO.

### Sources

- [MCP Specification — Authorization (2025-03-26)](https://modelcontextprotocol.io/specification/2025-03-26/basic/authorization)
- [MCP Specification — Security Best Practices](https://modelcontextprotocol.io/specification/draft/basic/security_best_practices)
- [Evolving OAuth Client Registration in MCP](https://blog.modelcontextprotocol.io/posts/client_registration/)
- [MCP Server Security: 7 OAuth 2.1 Best Practices](https://www.ekamoira.com/blog/secure-mcp-server-oauth-2-1-best-practices)
- [Technical Deconstruction of MCP Authorization (kane.mx)](https://kane.mx/posts/2025/mcp-authorization-oauth-rfc-deep-dive/)

---

## 2. Entra ID Multi-Hop OBO Architecture

Microsoft's On-Behalf-Of (OBO) flow is designed for exactly this scenario: **a middle-tier API calls a downstream API as the user**. Each API in the chain has its own app registration with its own scopes.

### Token flow

```
User (Browser)
  │
  ├─ 1. OIDC Authorization Code + PKCE
  │     → Entra ID issues id_token + access_token (audience: Web App)
  │
  ▼
Blazor Web App  [App Reg: LawCorp Web App]
  │
  ├─ 2. AcquireTokenOnBehalfOf(user_token, scope: api://<mcp-server>/access_as_user)
  │     → Entra ID issues OBO token (audience: MCP Server)
  │
  ▼
MCP Server API  [App Reg: LawCorp MCP Server]
  │
  ├─ 3a. AcquireTokenOnBehalfOf(obo_token, scope: api://<external-api>/data.read)
  │      → Entra ID issues OBO token (audience: External API)
  │
  ├─ 3b. AcquireTokenOnBehalfOf(obo_token, scope: https://graph.microsoft.com/...)
  │      → Entra ID issues OBO token (audience: Microsoft Graph)
  │
  ▼                                          ▼
External API  [App Reg: LawCorp External]    Microsoft Graph
  │                                            │
  ├─ Validates OBO token                       ├─ Returns user's M365 data
  ├─ Extracts user identity                    │
  ├─ Enforces its own authz                    │
  └─ Returns user-scoped data                  │
```

### Key Entra ID requirements for multi-hop OBO

1. **Each API needs its own app registration** with an `api://` Application ID URI and at least one delegated scope (e.g., `access_as_user`, `data.read`).
2. **The middle-tier API's app registration** must have **API permissions** for the downstream API's delegated scope, with admin consent granted.
3. **The middle-tier API needs a client secret or certificate** to perform the OBO exchange (the assertion is the inbound user token, the client credential proves the middle tier's identity).
4. **Token caching is critical** — each OBO exchange costs a round-trip to Entra ID. `Microsoft.Identity.Web` provides in-memory and distributed cache options.
5. **OBO only works with user-delegated tokens** — app-only tokens cannot be exchanged via OBO. This aligns with our "act as the user" requirement.

### Sources

- [Microsoft identity platform OBO flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)
- [MSAL.NET OBO flow](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow)
- [Web API that calls web APIs — acquire token](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-api-call-api-acquire-token)
- [Register applications — Zero Trust](https://learn.microsoft.com/en-us/security/zero-trust/develop/app-registration)
- [Configure protected web API apps](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-app-configuration)

---

## 3. Proposed App Registration Architecture

### Three app registrations (minimum for complete OBO demo)

| # | App Registration | Type | Purpose | Scopes Exposed | Scopes Consumed |
|---|---|---|---|---|---|
| 1 | **LawCorp Web App** | Web (OIDC) | MCP client — Blazor Web App | None (it's a client, not an API) | `api://<mcp-server>/access_as_user` |
| 2 | **LawCorp MCP Server** | Web API (JWT Bearer) | MCP resource provider — tool dispatch | `api://<mcp-server>/access_as_user` | `api://<external-api>/data.read`, Graph scopes |
| 3 | **LawCorp External API** | Web API (JWT Bearer) | Downstream API — independent data service | `api://<external-api>/data.read`, `api://<external-api>/data.write` | None (leaf node) |

### Scope design for the external API

| Scope | Permission Type | Purpose |
|---|---|---|
| `api://<external-api>/data.read` | Delegated | Read data the user has access to |
| `api://<external-api>/data.write` | Delegated | Write data the user has access to |

The external API can define its own app roles (if it needs role-based filtering) or rely solely on the user identity claims from the OBO token. For this reference architecture, both patterns should be demonstrated.

### App role propagation

App roles defined on the MCP server registration (Partner, Associate, etc.) are in the token the MCP server receives. When the MCP server performs OBO to the external API, the resulting token contains the **user's identity** but **not** the MCP server's app roles — the external API can define its own roles or read the user's group memberships from the token. This is an important architectural decision: **authorization boundaries are per-service, not inherited**.

---

## 4. Solution Structure Refactoring

### Current structure (everything in one solution)

```
src/
├── LawCorp.Mcp.Core/        ← Domain models, interfaces
├── LawCorp.Mcp.Data/        ← EF Core DbContext + migrations
├── LawCorp.Mcp.MockData/    ← Seed data generator
├── LawCorp.Mcp.Server/      ← MCP server (HTTP + stdio)
├── LawCorp.Mcp.Web/         ← Blazor web app (MCP client)
├── LawCorp.Mcp.Tests/       ← Unit tests
└── LawCorp.Mcp.Tests.E2E/   ← Playwright E2E
```

### Proposed structure (isolated services)

```
src/
├── LawCorp.Mcp.Core/              ← Shared domain models, interfaces, contracts
├── LawCorp.Mcp.Server/            ← MCP server API (no direct DB access)
│   ├── Tools/                     ← MCP tool classes (thin — dispatch via MediatR)
│   ├── Auth/                      ← Entra ID JWT + OBO + identity resolution
│   └── Infrastructure/            ← MediatR registration, HTTP clients for downstream APIs
├── LawCorp.Mcp.Server.Handlers/   ← CQRS command/query handlers (knows about data sources)
├── LawCorp.Mcp.Data/              ← EF Core DbContext + migrations
├── LawCorp.Mcp.MockData/          ← Seed data generator
├── LawCorp.Mcp.Web/               ← Blazor web app (MCP client)
├── LawCorp.Mcp.ExternalApi/       ← Independent downstream API service (NEW)
│   ├── Controllers/               ← REST endpoints
│   ├── Auth/                      ← Its own JWT Bearer validation
│   └── Data/                      ← Its own data access (could share DbContext or have its own)
├── LawCorp.Mcp.Tests/             ← Unit tests
└── LawCorp.Mcp.Tests.E2E/         ← Playwright E2E
```

### Key architectural decisions in this refactoring

| Decision | Recommendation | Rationale |
|---|---|---|
| Should the external API share the EF Core `DbContext`? | **Yes, for simplicity** — share `LawCorp.Mcp.Data` as a project reference. The external API exposes a subset of the same data through its own authorization lens. | Keeps the demo focused on auth patterns, not data model duplication. In production, the external API would have its own database. |
| Should the external API be a separate solution? | **No** — keep it in the same solution but as a separately runnable project (`dotnet run --project src/LawCorp.Mcp.ExternalApi`). | Demonstrates isolation without the overhead of separate repositories. The key point is separate app registrations and separate processes. |
| Should the MCP server still have direct DB access? | **Yes, alongside the external API path** — some tools read local DB (fast path), others call the external API (OBO path). MediatR handlers decide which. | Demonstrates both access patterns coexisting, which is the realistic enterprise scenario. |

---

## 5. CQRS / MediatR Dispatch Pattern

### Problem

Currently, MCP tools inject `DbContext` directly and execute queries inline. This works for a single-datasource demo but creates tight coupling:

```csharp
// Current pattern — tool knows about DbContext
[McpServerTool(Name = McpToolName.Cases.Search)]
public static async Task<string> SearchCases(DbContext db, IFirmIdentityContext identity, string query)
{
    var cases = await db.Cases.Where(c => c.Title.Contains(query)).ToListAsync();
    // ...
}
```

When some data comes from the external API, the tool would need to know whether to use `DbContext` or `HttpClient`, which scopes to request for OBO, how to handle API errors, etc. This is the wrong layer for those concerns.

### Solution: MediatR CQRS dispatch

Tools become thin dispatchers. They construct a command or query and send it via `IMediator`. The handler decides how to fulfill the request.

```csharp
// Proposed pattern — tool dispatches a query, handler resolves data source
[McpServerTool(Name = McpToolName.Cases.Search)]
public static async Task<string> SearchCases(IMediator mediator, string query)
{
    var result = await mediator.Send(new SearchCasesQuery(query));
    // Format result as MCP response
}

// Handler — knows about data source
public class SearchCasesHandler : IRequestHandler<SearchCasesQuery, SearchCasesResult>
{
    public async Task<SearchCasesResult> Handle(SearchCasesQuery request, CancellationToken ct)
    {
        // Could query local DB, call external API, or both
    }
}
```

### MediatR pipeline benefits for MCP

| Concern | MediatR Feature | Benefit |
|---|---|---|
| Authorization | `IPipelineBehavior` | Pre-handler check that the user's role permits this action — supplements `ToolPermissionFilters` |
| Audit logging | `IPipelineBehavior` | Log every command/query dispatch with user context — feeds into story 1.3.4 |
| Token acquisition | `IPipelineBehavior` or handler DI | Handler injects `IDownstreamTokenProvider` to acquire OBO token before calling external API |
| Caching | `IPipelineBehavior` | Cache query results per-user to reduce downstream API calls |
| Validation | `IPipelineBehavior` with FluentValidation | Validate command/query parameters before dispatch |

### Package

- [MediatR 12.x](https://www.nuget.org/packages/MediatR) — MIT license (note: 12.x introduced a licensing model; evaluate alternatives if needed)
- Alternative: hand-rolled dispatcher per [michaeldugmore.com](https://michaeldugmore.com/p/mediatr/) — lighter weight, no external dependency

### Sources

- [How to Implement CQRS with MediatR in .NET](https://oneuptime.com/blog/post/2026-01-28-cqrs-mediatr-dotnet/view)
- [MediatR documentation](https://mediatr.io/)
- [Ditching MediatR? Simple CQRS Dispatcher](https://michaeldugmore.com/p/mediatr/)

---

## 6. External API Service Design

### Purpose

The external API (`LawCorp.Mcp.ExternalApi`) represents an independent system that the MCP server calls on behalf of the user. It demonstrates:

1. **A separate app registration** with its own `api://` scope
2. **JWT Bearer validation** of OBO tokens from the MCP server
3. **Its own authorization logic** based on the user identity in the OBO token
4. **Independent deployment** — runs as a separate process on a different port

### What data does it serve?

To keep the demo realistic without inventing a new domain, the external API can expose a **subset of the law firm data** through a different access lens. Options:

| Option | Data | Rationale |
|---|---|---|
| **A. Case document service** | Documents and case files | Simulates a document management system (like iManage or NetDocuments) that the firm accesses through the MCP server |
| **B. Billing/time entry service** | Time entries, invoices, billing rates | Simulates an external billing system (like Clio or LEDES) |
| **C. Court calendar service** | Court dates, deadlines, filings | Simulates a court filing system integration |

**Recommendation: Option A (Case document service)** — documents are the most natural "external system" for a law firm, and the OBO flow is most compelling when the user's document access permissions are enforced by the external service, not the MCP server.

### API surface

```
GET  /api/documents?caseId={id}         → Documents for a case (user must have access)
GET  /api/documents/{id}                → Single document (user must have access)
GET  /api/documents/{id}/content        → Document content/download
POST /api/documents                     → Upload a document to a case
```

### Auth configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_EXTERNAL_API_CLIENT_ID",
    "Audience": "api://YOUR_EXTERNAL_API_CLIENT_ID"
  }
}
```

The external API validates the OBO token's `aud` claim matches its own client ID, extracts the user's `oid` and `roles` claims, and enforces authorization independently.

---

## 7. Proposed Project Management Changes

### New ADR

| ADR | Title | Decision |
|---|---|---|
| **ADR-008** | CQRS dispatch pattern for MCP tool handlers | Adopt MediatR (or hand-rolled dispatcher) so tools dispatch commands/queries without coupling to data sources |

### New/updated features and stories

#### Feature 1.2 (Authentication) — updates

| ID | Title | Type | Change |
|---|---|---|---|
| 1.2.3 | App registrations documentation | Task | **Update** — expand to cover three app registrations (web app, MCP server, external API) |

#### New feature: External API Service (under Epic 1 or new Epic)

| ID | Title | Type | Status |
|---|---|---|---|
| 1.4 (or new epic) | External Downstream API Service | Feature | PROPOSED |
| 1.4.1 | Create `LawCorp.Mcp.ExternalApi` project with JWT Bearer auth | Story | PROPOSED |
| 1.4.2 | Define app registration and `api://` scope for external API | Task | PROPOSED |
| 1.4.3 | Implement document API endpoints with OBO token validation | Story | PROPOSED |
| 1.4.4 | MCP server OBO token exchange to external API | Story | PROPOSED |
| 1.4.5 | Update `docs/auth-config.md` with third app registration | Task | PROPOSED |

#### New feature: CQRS Dispatch Layer

| ID | Title | Type | Status |
|---|---|---|---|
| 1.5 (or new epic) | CQRS Dispatch Pattern for MCP Tools | Feature | PROPOSED |
| 1.5.1 | Add MediatR and define command/query contracts in `LawCorp.Mcp.Core` | Story | PROPOSED |
| 1.5.2 | Create `LawCorp.Mcp.Server.Handlers` project with command/query handlers | Story | PROPOSED |
| 1.5.3 | Refactor existing tools to dispatch via `IMediator` | Story | PROPOSED |
| 1.5.4 | Implement pipeline behaviors (authorization, audit, validation) | Story | PROPOSED |
| 1.5.5 | Implement external API handler (dispatches OBO HTTP call) | Story | PROPOSED |

#### Updates to existing stories

| ID | Current Title | Proposed Change |
|---|---|---|
| 1.2.2 | OBO token exchange | **Expand scope** — must support OBO to custom downstream API, not just Graph |
| 1.2.4 | Downstream resource access | **Add sub-item** — downstream resource access via external API (not just Graph and local DB) |
| 3.2 | Document management tools | **Depends on** the external API service — document tools dispatch to the external API handler |

### Dependency graph

```
1.2.3 (app registrations docs)
  └─ 1.4.2 (external API app registration)

1.5.1 (MediatR contracts)
  └─ 1.5.2 (handlers project)
       ├─ 1.5.3 (refactor existing tools)
       └─ 1.5.5 (external API handler)
            └─ 1.4.3 (external API endpoints)
                 └─ 1.4.4 (MCP server OBO to external API)

1.4.1 (external API project)
  └─ 1.4.2 (app registration)
       └─ 1.4.3 (endpoints)
```

---

## 8. Implementation Phases

### Phase 1: Foundation (can start immediately)

1. **ADR-008:** Write the CQRS dispatch pattern ADR
2. **1.5.1:** Add MediatR to the solution, define initial command/query contracts in `LawCorp.Mcp.Core`
3. **1.4.1:** Create `LawCorp.Mcp.ExternalApi` project skeleton with JWT Bearer auth

### Phase 2: External API (depends on Phase 1)

4. **1.4.2:** Create the third app registration in Entra ID, define `api://` scope
5. **1.4.3:** Implement document API endpoints with OBO token validation
6. **1.2.2 (expand):** Add OBO token exchange configuration for the external API scope alongside the existing Graph scopes

### Phase 3: CQRS Dispatch (depends on Phase 1, can parallel Phase 2)

7. **1.5.2:** Create `LawCorp.Mcp.Server.Handlers` project
8. **1.5.3:** Refactor one or two existing tools (e.g., `SearchCases`) to dispatch via MediatR as a proof of concept
9. **1.5.4:** Implement pipeline behaviors (authorization, audit logging)

### Phase 4: Integration (depends on Phases 2 + 3)

10. **1.5.5:** Implement the external API handler — an `IRequestHandler` that acquires an OBO token and calls the external API
11. **1.4.4:** Wire up the MCP server's OBO token exchange for the external API scope
12. **3.2 (update):** Document management tools dispatch to the external API handler

### Phase 5: Documentation

13. **1.4.5:** Update `docs/auth-config.md` with the third app registration and multi-hop OBO flow
14. **1.2.3 (update):** Update the app registrations story to reflect three registrations
15. **Close BUG 1.2.3.1**

---

## 9. Open Questions

| # | Question | Options | Recommendation |
|---|---|---|---|
| 1 | Should we use MediatR or a hand-rolled dispatcher? | MediatR 12.x (MIT licensed, battle-tested) vs. custom `IDispatcher` interface | **MediatR** — ecosystem familiarity, pipeline behaviors, community adoption. Evaluate licensing impact. |
| 2 | Where do command/query contracts live? | `LawCorp.Mcp.Core` (shared) vs. `LawCorp.Mcp.Server.Contracts` (server-specific) | **`LawCorp.Mcp.Core`** — keeps the core project as the single place for all domain contracts. Handlers reference Core but don't depend on Server. |
| 3 | Should the external API have its own database? | Share `LawCorp.Mcp.Data` vs. separate DbContext | **Share for demo, separate in docs** — acknowledge that production would have separate databases. |
| 4 | Should the external API define its own app roles? | Own roles vs. rely on user identity claims | **Demonstrate both** — have roles on the external API but also show claim-based filtering without roles. |
| 5 | Does the MCP server still need direct DB access? | DB access via MediatR handlers vs. remove direct DbContext from tools | **Yes, keep both paths** — some handlers query local DB, others call external API. The MediatR handler is the decision point. |
| 6 | Should the external API support stdio transport? | HTTP only vs. HTTP + stdio | **HTTP only** — it's a standalone REST API, not an MCP server. Stdio is irrelevant. |
| 7 | How does this affect the Blazor web app? | No change vs. web app also calls external API directly | **No change** — the web app only talks to the MCP server. The MCP server is the facade. |

---

## 10. Authoritative References

### MCP Specification

| Source | URL |
|---|---|
| MCP Specification — Authorization (2025-03-26) | https://modelcontextprotocol.io/specification/2025-03-26/basic/authorization |
| MCP Security Best Practices | https://modelcontextprotocol.io/specification/draft/basic/security_best_practices |
| MCP Blog — Evolving OAuth Client Registration | https://blog.modelcontextprotocol.io/posts/client_registration/ |
| MCP OAuth 2.1 Best Practices (Ekamoira) | https://www.ekamoira.com/blog/secure-mcp-server-oauth-2-1-best-practices |
| MCP Authorization Deep Dive (kane.mx) | https://kane.mx/posts/2025/mcp-authorization-oauth-rfc-deep-dive/ |
| MCP Authorization Spec Analysis (den.dev) | https://den.dev/blog/new-mcp-authorization-spec |

### Microsoft Entra ID / OBO

| Source | URL |
|---|---|
| OAuth 2.0 On-Behalf-Of flow | https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow |
| MSAL.NET OBO flow | https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/web-apps-apis/on-behalf-of-flow |
| Web API that calls web APIs — acquire token | https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-api-call-api-acquire-token |
| Register applications (Zero Trust) | https://learn.microsoft.com/en-us/security/zero-trust/develop/app-registration |
| Configure protected web API apps | https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-app-configuration |
| Microsoft.Identity.Web overview | https://learn.microsoft.com/en-us/entra/msal/dotnet/microsoft-identity-web/ |

### Azure AI Foundry / MCP

| Source | URL |
|---|---|
| Build and register MCP server (Foundry) | https://learn.microsoft.com/en-us/azure/ai-foundry/mcp/build-your-own-mcp-server |
| Agent-to-Agent authentication (Foundry) | https://learn.microsoft.com/en-us/azure/ai-foundry/agents/concepts/agent-to-agent-authentication |
| Securing MCP Servers with Azure APIM | https://medium.com/@roeyzalta/securing-mcp-servers-in-production-with-azure-api-management-b7b22bba5d72 |

### CQRS / MediatR

| Source | URL |
|---|---|
| MediatR documentation | https://mediatr.io/ |
| MediatR NuGet (12.3.0) | https://www.nuget.org/packages/MediatR |
| CQRS with MediatR in .NET (2026) | https://oneuptime.com/blog/post/2026-01-28-cqrs-mediatr-dotnet/view |
| Simple CQRS Dispatcher (MediatR alternative) | https://michaeldugmore.com/p/mediatr/ |

### IETF RFCs (referenced by MCP spec)

| RFC | Title | URL |
|---|---|---|
| RFC 9728 | OAuth 2.0 Protected Resource Metadata | https://datatracker.ietf.org/doc/html/rfc9728/ |
| RFC 8414 | OAuth 2.0 Authorization Server Metadata | https://datatracker.ietf.org/doc/html/rfc8414/ |
| RFC 7636 | PKCE for OAuth 2.0 | https://datatracker.ietf.org/doc/html/rfc7636/ |
| RFC 8707 | Resource Indicators for OAuth 2.0 | https://datatracker.ietf.org/doc/html/rfc8707/ |
| RFC 7591 | OAuth 2.0 Dynamic Client Registration | https://datatracker.ietf.org/doc/html/rfc7591/ |
