# ADR-006: Add a white-labelled web application as the MCP client demo and E2E test harness

**Status:** Accepted
**Date:** 2026-02-24

## Context

The Law-Corp MCP server is an enterprise reference architecture for .NET MCP server implementations. Today it can be exercised through three client surfaces:

1. **Claude Desktop** — stdio transport, manual testing only, no auth
2. **MCP Inspector** — HTTP transport, manual token acquisition via `az cli`, paste-the-token workflow ([docs/local-mcp-inspect-auth.md](../../docs/local-mcp-inspect-auth.md))
3. **xUnit integration tests** — `WebApplicationFactory` with `FakeIdentityContext`, no real Entra tokens

None of these surfaces support what a real enterprise deployment requires: **a user logs into a web UI with their corporate identity, invokes MCP tools through a conversational or task-oriented interface, and every downstream action executes under that user's delegated access token — verifiable end-to-end with automated tests.**

Specific gaps:

- **No browser-based login flow.** The MCP Inspector requires manual token acquisition. There is no UI where a user authenticates via Entra ID's standard redirect flow and then interacts with MCP tools.
- **No Playwright-scriptable E2E path.** Without a web app, there is no browser to automate. Playwright needs a login page → authenticated session → tool invocation → result assertion path.
- **No demo surface for stakeholders.** Showing the identity passthrough value proposition ([ADR-005](./005-oauth-identity-passthrough.md)) currently requires a terminal, `az cli`, and pasting JWTs — not accessible to non-developers.
- **No audit visibility.** A web app can display the full-stack communication trace: inbound token → identity resolution → tool invocation → downstream access → filtered results — making authorization decisions visible.

### What we need

A **white-labelled web application** that:

1. Authenticates the user via Entra ID (standard OIDC redirect flow)
2. Connects to the Law-Corp MCP server as an MCP client (HTTP/SSE transport)
3. Passes the user's access token to the MCP server on every tool call (identity passthrough)
4. Provides a UI for invoking MCP tools and viewing results (conversational or task-oriented)
5. Displays authorization audit trails (what identity was used, what access was granted/denied)
6. Is automatable via Playwright for E2E testing with real Entra ID personas

### Framework candidates

Three frameworks were evaluated based on alignment with the existing solution, MCP client SDK availability, Entra ID integration maturity, and Playwright testability.

---

## Option A: Blazor Web App (.NET 9, Interactive Server)

### Architecture

```
LawCorp.Mcp.Web (new project)
  ├── Blazor Web App (.NET 9, Interactive Server render mode)
  ├── References LawCorp.Mcp.Core (shared types, IUserContext, IFirmIdentityContext)
  ├── Uses ModelContextProtocol C# SDK (HttpClientTransport) as MCP client
  ├── Authenticates via Microsoft.Identity.Web (OIDC redirect → Entra ID)
  ├── Passes user token to MCP server via Bearer header on HttpClientTransport
  └── Runs as a separate ASP.NET Core process alongside LawCorp.Mcp.Server
```

### How MCP client integration works

The C# MCP SDK provides `HttpClientTransport` for connecting to remote MCP servers. The Blazor app creates an `IMcpClient` per user session:

```csharp
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", userAccessToken);

var transport = new HttpClientTransport(
    new HttpClientTransportOptions { Endpoint = new Uri("http://localhost:5000/mcp") },
    httpClient);

var client = await McpClientFactory.CreateAsync(transport);

// Invoke tools under the user's identity
var result = await client.CallToolAsync("cases_search", new { query = "merger" });
```

The `userAccessToken` is the Entra ID access token obtained during the OIDC login, scoped to `api://<MCP-server-client-id>/access_as_user`. The MCP server receives it as a Bearer header and resolves the user's identity via `UserContextResolutionMiddleware`.

### Strengths

| Criterion | Assessment |
|---|---|
| **Stack alignment** | Same language (C#), same framework (ASP.NET Core), same package manager (NuGet). One team, one skillset. The stated goal is a **.NET reference architecture** — Blazor keeps it unified. |
| **Type sharing** | The web app project references `LawCorp.Mcp.Core` directly. Domain models (`AttorneyRole`, `IUserContext`, persona definitions) are shared, not duplicated. |
| **MCP client SDK** | Official C# MCP SDK with `HttpClientTransport`. No JavaScript wrapper or interop needed. The client runs server-side in the Blazor Server model. |
| **Auth integration** | `Microsoft.Identity.Web` handles OIDC redirect, token acquisition, token cache, and OBO exchange natively. Same library the MCP server uses. No MSAL.js. |
| **Playwright compatibility** | Playwright works with any web framework. Blazor Server renders standard HTML over SignalR. Login flow automation follows the established `auth.setup.ts` + `storageState.json` pattern. |
| **E2E test hosting** | Both the MCP server and the Blazor client can be hosted in-process via `WebApplicationFactory<Program>` in xUnit. True E2E tests without spawning separate processes. |
| **Server-side MCP client** | In the Interactive Server model, the MCP client connection lives on the server — not in the browser. This means: (a) tokens stay server-side (more secure), (b) `HttpClientTransport` works natively (no browser CORS), (c) streaming MCP responses push to the browser via SignalR. |
| **White-labelling** | Blazor's component model + CSS isolation makes theming straightforward. Fluent UI Blazor or MudBlazor provide enterprise-ready component libraries. |

### Weaknesses

| Concern | Mitigation |
|---|---|
| Blazor ecosystem smaller than React | This is a demo/test shell, not a production SPA. Component library maturity is sufficient (MudBlazor, Fluent UI Blazor, Radzen). |
| Interactive Server requires SignalR | For a demo/internal tool this is a non-issue. The persistent connection actually benefits streaming MCP responses. For production, Blazor WASM or Auto render mode are options. |
| Less familiar to frontend-focused teams | The web app is the test harness, not the product. Teams adopting the MCP server can substitute their own frontend. |
| No official "use-mcp for Blazor" hook | The C# MCP SDK's `McpClientFactory` + `HttpClientTransport` provides the equivalent capability. A thin Blazor service wrapper is straightforward to build. |

---

## Option B: React + TypeScript (Vite)

### Architecture

```
web/ (new folder at repo root)
  ├── React 18 + TypeScript + Vite + Tailwind CSS
  ├── Uses @modelcontextprotocol/sdk or use-mcp React hook
  ├── Authenticates via @azure/msal-react (OIDC redirect → Entra ID)
  ├── Connects to MCP server via HTTP/SSE transport with Bearer token
  └── Runs as a separate Node.js dev server or built as static assets
```

### How MCP client integration works

The `use-mcp` React hook provides MCP client connectivity with built-in OAuth and reconnection:

```tsx
import { useMcp } from 'use-mcp/react';

function ToolPanel() {
  const { state, tools, callTool } = useMcp({
    url: 'http://localhost:5000/mcp',
    clientName: 'LawCorp Web',
    autoReconnect: true,
  });
  // ...
}
```

For Entra ID auth, `@azure/msal-react` handles the redirect flow, and the acquired token must be injected into the MCP transport's HTTP headers.

The MCP organization's `example-remote-client` provides a production reference for this pattern — React 18, Vite, Tailwind, multi-server MCP connections, conversational UI with tool calling.

### Strengths

| Criterion | Assessment |
|---|---|
| **MCP ecosystem** | `use-mcp` hook and `example-remote-client` are first-party React implementations from the MCP organization. Proven pattern. |
| **Frontend ecosystem** | Largest component library ecosystem (shadcn/ui, Radix, Material UI). Most frontend developers know React. |
| **Client-side MCP** | MCP client runs entirely in the browser. No server-side session state. Simpler horizontal scaling. |
| **Playwright compatibility** | Excellent. React + Playwright is the most common E2E testing combination. |

### Weaknesses

| Concern | Impact |
|---|---|
| **Mixed stack** | Adds TypeScript/Node.js tooling to a .NET project. Two languages, two package managers, two build pipelines. Dilutes the "reference architecture for .NET" narrative. |
| **No type sharing** | Domain models (`AttorneyRole`, persona definitions) must be duplicated or generated. Drift risk. |
| **Separate auth library** | MSAL.js is a different library from `Microsoft.Identity.Web`. Two auth configurations to maintain. Token management (cache, refresh, OBO) must be handled in JavaScript. |
| **CORS and token handling** | Browser-based MCP client requires CORS on the MCP server. Bearer tokens are handled in JavaScript (XSS surface). |
| **E2E test hosting** | Cannot use `WebApplicationFactory`. Tests must spawn two separate processes (MCP server + React dev server) or serve built static assets from ASP.NET Core. |
| **Increased onboarding** | Contributors need Node.js, npm/pnpm, and React knowledge in addition to .NET. |

---

## Option C: Angular

### Architecture

```
web/ (new folder at repo root)
  ├── Angular 19 + TypeScript
  ├── Uses @modelcontextprotocol/sdk (TypeScript) directly (no Angular-specific MCP library)
  ├── Authenticates via @azure/msal-angular (OIDC redirect → Entra ID)
  └── Runs as a separate Node.js dev server or built as static assets
```

### Strengths

| Criterion | Assessment |
|---|---|
| **Enterprise conventions** | Opinionated framework with built-in dependency injection, routing, forms, HTTP client. Appeals to enterprise .NET teams familiar with structured frameworks. |
| **MSAL Angular** | Microsoft maintains `@azure/msal-angular` with first-party support. |
| **Playwright** | Works well. Angular + Playwright is a supported testing combination. |

### Weaknesses

| Concern | Impact |
|---|---|
| **No MCP Angular library** | Would use the raw TypeScript SDK. More integration work than React's `use-mcp` hook. |
| **Same mixed-stack issues as React** | Two languages, no type sharing, separate auth config, CORS, separate processes. |
| **Heavier framework** | More boilerplate than React for a demo/test shell. |
| **Steeper learning curve** | RxJS, decorators, modules — adds cognitive load for a secondary project component. |

---

## Other options considered

| Option | Why not |
|---|---|
| **Blazor WebAssembly** | MCP client would run in the browser (WASM). `HttpClientTransport` works but tokens are in the browser (XSS surface). Large initial download (~5 MB). WASM startup latency. For a demo/test shell, Interactive Server is simpler and more secure. |
| **Blazor Auto (Server + WASM)** | Adds complexity of two render modes. The web app is internal/demo — no need for offline support or CDN-served static assets. |
| **MAUI Blazor Hybrid** | Desktop app, not a web app. Can't be Playwright-tested. Out of scope. |
| **Next.js / Remix** | React meta-frameworks with SSR. Adds server-side TypeScript complexity on top of the React tradeoffs. Over-engineered for this use case. |
| **Svelte / Vue** | Smaller ecosystems. No official MCP hooks. No MSAL library. Not in the "Blazor, Angular, React" consideration set. |
| **Plain HTML + vanilla JS** | Minimalist. Could use the TypeScript SDK directly. But no component model, no auth library integration, poor developer experience. Not a reference-quality demo. |

---

## Decision

**Use Blazor Web App (.NET 9) with Interactive Server render mode** as the web application framework.

### Rationale summary

The decisive factors are **stack coherence** and **E2E testability**:

1. **One stack.** The project's stated purpose is a .NET MCP server reference architecture. Adding React or Angular introduces a second language, a second build pipeline, and a second auth configuration. Blazor keeps the entire solution in C# / .NET 9 / NuGet / MSBuild.

2. **Shared types.** The web app project references `LawCorp.Mcp.Core` and shares `IUserContext`, `AttorneyRole`, persona definitions, and all domain models. No duplication, no drift, no code generation.

3. **Native MCP client.** The C# MCP SDK's `HttpClientTransport` connects to the MCP server from the Blazor Server backend. No JavaScript MCP library, no CORS, no browser-side token handling. The MCP client connection and the user's access token both live server-side.

4. **Native auth.** `Microsoft.Identity.Web` handles OIDC redirect, token cache, and OBO exchange. Same library, same configuration pattern as the MCP server. One `AzureAd` config section, not two separate MSAL setups.

5. **In-process E2E.** Both `LawCorp.Mcp.Server` and `LawCorp.Mcp.Web` can be hosted via `WebApplicationFactory<Program>` in xUnit. Playwright drives the Blazor UI, which calls the MCP server — all in one test process. No Docker, no separate dev servers, no port coordination.

6. **Playwright scripts.** Playwright automates the Entra ID login redirect (email + password → consent → redirect back). The `storageState.json` pattern caches the signed-in state so subsequent tests skip login. Each persona gets its own storage state file, enabling per-persona test runs.

### Solution structure

```
src/
  LawCorp.Mcp.sln
  LawCorp.Mcp.Core/          ← shared domain models, interfaces
  LawCorp.Mcp.Data/          ← EF Core, DbContext
  LawCorp.Mcp.MockData/      ← mock data generator, persona seeder
  LawCorp.Mcp.Server/        ← MCP server (ASP.NET Core, tools, auth middleware)
  LawCorp.Mcp.Web/           ← NEW: Blazor Web App (MCP client, UI, Entra login)
  LawCorp.Mcp.Tests/         ← unit + integration tests
  LawCorp.Mcp.Tests.E2E/     ← NEW: Playwright E2E tests (browser automation)
```

### Key architectural decisions within the web app

| Decision | Choice | Rationale |
|---|---|---|
| **Render mode** | Interactive Server | MCP client runs server-side. Tokens stay server-side. SignalR streams results to browser in real-time. No WASM download. |
| **MCP client** | C# SDK `HttpClientTransport` | Same SDK as the server, no JavaScript interop. Per-session `IMcpClient` with the user's Bearer token. |
| **Auth library** | `Microsoft.Identity.Web` | OIDC redirect + token cache + OBO. Same as MCP server. |
| **Component library** | Fluent UI Blazor (recommended) or MudBlazor | Enterprise look-and-feel. White-labelling via CSS custom properties. Fluent UI aligns with Microsoft ecosystem. |
| **Playwright tests** | `LawCorp.Mcp.Tests.E2E` project | Separate xUnit project with `[Trait("Category", "E2E")]`. Uses `auth.setup.ts`-equivalent for Entra login state caching per persona. |

### Authentication flow

```
User opens Blazor Web App
  │
  ├─ 1. OIDC redirect → Entra ID login page
  ├─ 2. User enters email + password (+ MFA if configured)
  ├─ 3. Entra ID redirects back with authorization code
  ├─ 4. Blazor backend exchanges code for access token (audience: api://<MCP-server-client-id>/access_as_user)
  ├─ 5. Token cached server-side (Microsoft.Identity.Web token cache)
  │
  ├─ 6. User invokes MCP tool via Blazor UI
  │     │
  │     ▼
  │  Blazor Server → HttpClientTransport (Bearer: <user-token>) → MCP Server
  │     │
  │     ▼
  │  MCP Server validates token → resolves identity → executes tool → returns result
  │
  └─ 7. Result streams to browser via SignalR → displayed in UI
```

### Playwright E2E flow

```
Playwright test script
  │
  ├─ 1. auth.setup: Navigate to Blazor app login → redirect to Entra → fill email/password → redirect back
  ├─ 2. Save storageState to .auth/harvey-specter.json
  │
  ├─ 3. Test: Load Blazor app with Harvey's storage state (already logged in)
  ├─ 4. Navigate to tool invocation page
  ├─ 5. Invoke cases_search → assert: sees all M&A cases
  │
  ├─ 6. Repeat with Kim Wexler's storage state
  ├─ 7. Invoke cases_search → assert: sees only assigned cases
  │
  └─ 8. Compare: Harvey sees more cases than Kim → identity passthrough verified
```

### If a team prefers React or Angular

The MCP server is framework-agnostic. Any web app that can:

1. Authenticate via Entra ID (OIDC redirect)
2. Acquire a token scoped to `api://<client-id>/access_as_user`
3. Connect to the MCP server via HTTP with `Authorization: Bearer <token>`

...will work. For React specifically, the MCP organization's `use-mcp` hook and `example-remote-client` provide a production-ready starting point. For Angular, the TypeScript SDK works with `@azure/msal-angular`.

The Blazor web app in this repo is the first reference implementation — teams are expected to substitute their own frontend.

### Future: Angular and React companion apps

To demonstrate the framework-agnostic nature of the MCP server and provide concrete comparison points, we plan to build **Angular** and **React** versions of the web app in future phases:

| Phase | Framework | Purpose |
|---|---|---|
| **Current (Epic 8)** | Blazor Web App (.NET 9) | Primary reference — single-stack, type sharing, in-process E2E |
| **Future** | Angular 19 + TypeScript | Comparison point — enterprise TypeScript framework, `@azure/msal-angular`, raw TS MCP SDK |
| **Future** | React 18 + TypeScript (Vite) | Comparison point — `use-mcp` hook, `@azure/msal-react`, largest ecosystem |

All three apps will connect to the same `LawCorp.Mcp.Server` and run the same Playwright E2E persona test suite. This will produce a direct, evidence-based comparison of:

- **Auth integration complexity** (MSAL.js vs. Microsoft.Identity.Web)
- **MCP client ergonomics** (use-mcp hook vs. C# HttpClientTransport vs. raw TS SDK)
- **Type safety** (shared .NET types vs. generated/duplicated TypeScript types)
- **E2E test hosting** (in-process WebApplicationFactory vs. multi-process with CORS)
- **Build pipeline complexity** (single dotnet build vs. mixed NuGet + npm)

The Angular and React apps will be tracked as separate epics when their planning begins. They are not prerequisites for the Blazor app or any other current work.

## Consequences

**Easier:**
- Full-stack .NET solution — one language, one build, one CI pipeline
- Shared domain types eliminate model drift between client and server
- `WebApplicationFactory` enables in-process E2E testing of both client and server
- Playwright scripts provide automated persona-based access control verification
- Stakeholder demos require only a browser — no terminal, no `az cli`, no pasted JWTs
- The web app doubles as a test harness and a demo surface
- White-labelling is straightforward with Blazor CSS isolation + Fluent UI design tokens

**Harder:**
- Adds a new project (`LawCorp.Mcp.Web`) and test project (`LawCorp.Mcp.Tests.E2E`) to the solution
- Blazor Interactive Server requires a persistent SignalR connection — not suitable for high-scale production, but fine for a reference architecture / demo
- Teams unfamiliar with Blazor need to learn its component model (though the learning curve from ASP.NET Core is small)
- Requires a second Entra ID app registration (for the web app) or a multi-platform registration with redirect URIs for both the web app and the MCP Inspector
- Playwright tests against real Entra ID require a test tenant with pre-created users and MFA disabled (or conditional access bypass)

**New work items:**
- [Epic 8: Web Application](../epics/08-web-app/_epic.md) — Blazor project foundation, MCP client integration, auth audit UI, white-labelling, Playwright E2E tests

**Open questions:**
- Should the web app and the MCP server share a single Entra app registration (with multiple redirect URIs) or use separate registrations? Separate is cleaner for token audience isolation but adds setup complexity.
- Should the Blazor app expose a "raw MCP message" debug view (showing JSON-RPC requests/responses) for educational/debugging purposes?
- Should the Playwright E2E tests run against a shared test tenant with pre-provisioned users, or should they use ROPC (Resource Owner Password Credentials) for non-interactive login? ROPC is simpler to automate but is deprecated for production and doesn't work with MFA.
- Should the web app include an LLM conversational interface (like `example-remote-client`) or focus purely on tool invocation? A conversational interface is a richer demo but adds LLM provider configuration (OpenAI/Azure OpenAI API key).

## References

- [MCP C# SDK — HttpClientTransport](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Client.HttpClientTransport.html)
- [MCP C# SDK — McpClientFactory](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Client.html)
- [use-mcp — React hook for MCP](https://github.com/modelcontextprotocol/use-mcp)
- [example-remote-client — Official MCP React client](https://github.com/modelcontextprotocol/example-remote-client)
- [Microsoft.Identity.Web — Blazor Server auth](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-app-call-api-overview)
- [Playwright — Handling Azure AD/Entra ID Authentication](https://marcusfelling.com/blog/2023/handling-azure-ad-authentication-with-playwright/)
- [Fluent UI Blazor](https://www.fluentui-blazor.net/)
- [ADR-005: OAuth identity passthrough](./005-oauth-identity-passthrough.md)
- [ADR-004: Dual transport Web API](./004-dual-transport-web-api-primary.md)
- [docs/auth-config.md](../../docs/auth-config.md)
- [docs/local-mcp-inspect-auth.md](../../docs/local-mcp-inspect-auth.md)
