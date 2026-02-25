# LawCorp.Mcp.Web

A Blazor Web App (.NET 9, Interactive Server) that serves as the demo and E2E test harness for the Law-Corp MCP server. It provides a white-labelled UI for invoking MCP tools, browsing resources and prompts, and auditing authorization decisions — all authenticated via Microsoft Entra ID.

See [ADR-006](../../proj-mgmt/decisions/006-web-app-architecture.md) for the full architecture decision and rationale.

## Architecture

| Concern | Approach |
|---|---|
| **Render mode** | Interactive Server — keeps tokens server-side, real-time updates via SignalR |
| **Authentication** | OIDC via `Microsoft.Identity.Web` → Entra ID |
| **Token passthrough** | User's access token forwarded to the MCP server (`Authorization: Bearer`) |
| **Component library** | [Fluent UI Blazor](https://www.fluentui-blazor.net/) for enterprise look and feel |
| **White-labelling** | Branding and theming driven by `appsettings.json` |

## Project References

```
Web  →  Core  (shared domain models and auth interfaces)
```

## Quick Start

**Prerequisites**
- .NET 9 SDK
- The MCP server running in HTTP mode (`Transport=http` at `http://localhost:5000`)

**Run (no auth)**

```bash
cd src
dotnet run --project LawCorp.Mcp.Web --launch-profile https
```

The app launches at `https://localhost:5001`. With `UseAuth=false` (the default), authentication is bypassed and all pages are accessible without sign-in.

**Run (with Entra ID auth)**

1. Copy `appsettings.Development.json.example` to `appsettings.Development.json`
2. Fill in values from your two Entra ID app registrations (see [Configuration](#configuration) below)
3. Run:

```bash
dotnet run --project LawCorp.Mcp.Web --launch-profile https
```

You will see a "Sign in" button. After authenticating with a persona at Entra ID, the home page displays your identity and role.

---

## Configuration

### Why does the web app have a client secret?

This Blazor app uses **Interactive Server** render mode. All application code — including authentication, token exchange, and API calls — runs on the server. The browser receives only a SignalR connection for UI updates; it never sees tokens or secrets.

This makes the web app a **confidential client** in OAuth terms, which is the same category as a traditional server-rendered MVC app or a backend API. Confidential clients are trusted to hold secrets because no code or configuration is shipped to the user's browser. This is fundamentally different from:

- **Blazor WebAssembly** — runs in the browser, is a public client, must NOT have secrets
- **React / Angular SPAs** — run in the browser, are public clients, use PKCE instead of secrets
- **Mobile apps** — are public clients, use PKCE

The `ClientSecret` in our `appsettings` is used server-side for two purposes:
1. **OIDC authorization code exchange** — after the user authenticates at Entra ID, the server exchanges the authorization code for tokens using the secret
2. **Downstream token acquisition** — the server acquires tokens to call the MCP server API on behalf of the user (On-Behalf-Of flow) using the secret

> **Security:** The committed `appsettings.json` contains empty placeholders. Real secrets live in `appsettings.Development.json`, which is gitignored. In production, use Azure Key Vault or managed identity certificates instead of plain-text secrets.

### App registrations overview

This solution requires **two separate Entra ID app registrations** plus **test user accounts** (personas). Each registration serves a different role:

```
┌──────────────────────────────────┐
│  Entra ID Tenant                 │
│                                  │
│  ┌────────────────────────────┐  │
│  │ Registration 1:            │  │
│  │ "LawCorp MCP Server"      │  │
│  │  - Web API (no redirect)  │  │
│  │  - Exposes scope:         │  │
│  │    access_as_user          │  │
│  │  - Defines app roles:     │  │
│  │    Partner, Associate, ... │  │
│  │  - Has client secret for  │  │
│  │    OBO token exchange      │  │
│  └──────────┬─────────────────┘  │
│             │ "call on behalf    │
│             │  of user"          │
│  ┌──────────┴─────────────────┐  │
│  │ Registration 2:            │  │
│  │ "LawCorp Web App"         │  │
│  │  - Web app (confidential) │  │
│  │  - Redirect URI:          │  │
│  │    https://localhost:5001  │  │
│  │    /signin-oidc            │  │
│  │  - Has API permission to  │  │
│  │    call MCP Server scope   │  │
│  │  - Has client secret for  │  │
│  │    OIDC code exchange      │  │
│  └────────────────────────────┘  │
│                                  │
│  ┌────────────────────────────┐  │
│  │ Test Users (Personas)      │  │
│  │  harvey@tenant / Partner   │  │
│  │  kim@tenant / Associate    │  │
│  │  alan@tenant / OfCounsel   │  │
│  │  erin@tenant / Paralegal   │  │
│  │  elle@tenant / LegalAsst   │  │
│  │  vinny@tenant / Intern     │  │
│  │  Assigned to app roles on  │  │
│  │  BOTH registrations        │  │
│  └────────────────────────────┘  │
└──────────────────────────────────┘
```

The personas are real Entra ID user accounts created in your test tenant. Their Object IDs are mapped to attorney records in the local database via `persona-seed.json` (see [Personas README](../LawCorp.Mcp.MockData/Personas/README.md)). Each persona is assigned to an app role on both registrations so the `roles` claim appears in tokens issued for either app.

### Settings reference

Settings live in two files:
- **`appsettings.json`** (committed) — empty placeholders and non-secret defaults
- **`appsettings.Development.json`** (gitignored) — your tenant's real values

Copy `appsettings.Development.json.example` → `appsettings.Development.json` and fill in values from the two app registrations.

#### `UseAuth`

| Value | Behaviour |
|---|---|
| `false` (default) | No authentication. All pages accessible. `<AuthorizeView>` shows "Not signed in" state. Use for UI development without an Azure tenant. |
| `true` | Full Entra ID OIDC. "Sign in" button redirects to Microsoft login. Tokens are acquired and cached server-side. |

#### `AzureAd` — Web App registration values

These come from **Registration 2** ("LawCorp Web App"):

| Key | Source | Description |
|---|---|---|
| `Instance` | Fixed | `https://login.microsoftonline.com/` — the Entra ID authority base URL |
| `TenantId` | Azure Portal → App registration → Overview → **Directory (tenant) ID** | Your Azure AD tenant. Same value used in both registrations. |
| `ClientId` | Azure Portal → "LawCorp Web App" → Overview → **Application (client) ID** | Identifies this web app to Entra ID. This is the **web app's** client ID, not the MCP server's. |
| `ClientSecret` | Azure Portal → "LawCorp Web App" → Certificates & secrets → **Client secret value** | Used server-side for OIDC code exchange and downstream token acquisition. Never reaches the browser. See [Why does the web app have a client secret?](#why-does-the-web-app-have-a-client-secret) |
| `CallbackPath` | Fixed | `/signin-oidc` — the redirect URI path that Entra ID posts the authorization code to after login. Must match the redirect URI configured in the app registration. |
| `SignedOutCallbackPath` | Fixed | `/signout-callback-oidc` — the path Entra ID redirects to after sign-out completes. |

#### `McpServer` — MCP Server registration values

These reference **Registration 1** ("LawCorp MCP Server"):

| Key | Source | Description |
|---|---|---|
| `Endpoint` | Your MCP server's URL | `http://localhost:5000/mcp` for local development. The web app calls this endpoint with the user's token. |
| `Scopes` | Azure Portal → "LawCorp MCP Server" → Expose an API → **Scope URI** | See [Scope URI anatomy](#scope-uri-anatomy) below. |

##### Scope URI anatomy

The `McpServer:Scopes` value follows the format `api://<MCP-SERVER-CLIENT-ID>/access_as_user`:

```
api://d1234567-abcd-ef01-2345-6789abcdef01/access_as_user
└─┬─┘ └──────────────┬───────────────────┘ └──────┬──────┘
scheme   MCP server's Application (client) ID      scope name
         (from Registration 1 → Overview)
```

| Part | What it is | Where to find it |
|---|---|---|
| `api://` | Entra ID's scheme for custom API scopes | Fixed — always `api://` |
| `d1234567-...` | The **MCP server's** Application (client) ID | Azure Portal → "LawCorp MCP Server" → Overview |
| `access_as_user` | A delegated permission defined on the server registration | Azure Portal → "LawCorp MCP Server" → Expose an API |

The web app requests this scope when it acquires a token to call the MCP server on behalf of the signed-in user. The MCP server validates that inbound tokens include this scope in their `scp` claim.

> **Common mistake:** Using the web app's client ID instead of the MCP server's. The GUID in the scope URI is always the **API being called** (the MCP server), not the **caller** (the web app). If you use the wrong ID, token acquisition will fail with an `AADSTS650053` or `AADSTS70011` error.

#### `Branding` — UI customisation

| Key | Description |
|---|---|
| `AppName` | App title shown in the header and browser tab |
| `Tagline` | Subtitle on the home page |
| `FooterText` | Footer text |

### Example: filled-in `appsettings.Development.json`

```json
{
  "UseAuth": true,
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "6a887b03-...",
    "ClientId": "eaf857c8-...",
    "ClientSecret": "abc123~secret"
  },
  "McpServer": {
    "Endpoint": "http://localhost:5000/mcp",
    "Scopes": [ "api://d1234567-abcd-.../access_as_user" ]
  }
}
```

Note how `AzureAd:ClientId` and the Client ID inside `McpServer:Scopes` are **different values** — one is the web app, the other is the MCP server.

---

## Pages

| Route | Purpose | Status |
|---|---|---|
| `/` | Home — identity display and capability overview | Implemented |
| `/account/claims` | JWT claims debug table — verify `oid`, `roles`, `preferred_username` | Implemented |
| `/tools` | MCP tool discovery and invocation | Placeholder (Feature 8.2) |
| `/resources` | MCP resource browser | Placeholder (Feature 8.2) |
| `/prompts` | MCP prompt template browser | Placeholder (Feature 8.2) |
| `/trace` | MCP JSON-RPC message trace viewer | Placeholder (Feature 8.3) |
| `/audit` | Authorization decision audit log | Placeholder (Feature 8.3) |

## Related Documentation

- [ADR-006: Web App Architecture](../../proj-mgmt/decisions/006-web-app-architecture.md)
- [ADR-005: OAuth Identity Passthrough](../../proj-mgmt/decisions/005-oauth-identity-passthrough.md)
- [Story 8.1.2: Entra ID OIDC Sign-In](../../proj-mgmt/epics/08-web-app/8.1.2-entra-id-oidc-auth/8.1.2-entra-id-oidc-auth.md)
- [Epic 8: Web Application](../../proj-mgmt/epics/08-web-app/_epic.md)
- [Auth Config Guide](../../docs/auth-config.md) — Step 10 covers the web app setup
- [Testing Auth with MCP Inspector](../../docs/local-mcp-inspect-auth.md) — includes web app as alternative
- [Personas README](../LawCorp.Mcp.MockData/Personas/README.md)
