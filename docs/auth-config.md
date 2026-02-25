# Authentication Configuration Guide

This guide walks through configuring Microsoft Entra ID authentication for the Law-Corp MCP solution. The architecture uses **three Entra ID app registrations** to demonstrate enterprise-grade, multi-hop On-Behalf-Of (OBO) identity delegation:

1. **LawCorp MCP Server** — the MCP resource provider (JWT Bearer, OBO to downstream APIs)
2. **LawCorp Web App** — the MCP client / Blazor Web App (OIDC authorization code)
3. **LawCorp External API** — an independent downstream API called by the MCP server via OBO

When complete, every MCP tool call executes under the calling user's identity — the server only sees and modifies data the user is personally authorized to access, whether that data lives in a local database, Microsoft Graph, or the external API.

**Related:**
- [ADR-005: OAuth identity passthrough](../proj-mgmt/decisions/005-oauth-identity-passthrough.md) — architectural rationale
- [ADR-006: Web app architecture](../proj-mgmt/decisions/006-web-app-architecture.md) — Blazor web app decision
- [ADR-008: CQRS dispatch pattern](../proj-mgmt/decisions/008-cqrs-dispatch-pattern.md) — MediatR tool dispatch
- [Story 1.2.4: Downstream resource access](../proj-mgmt/epics/01-foundation/1.2-authn/1.2.4-downstream-resource-access/1.2.4-downstream-resource-access.md) — user story
- [Story 1.2.1: Entra ID auth middleware](../proj-mgmt/epics/01-foundation/1.2-authn/1.2.1-entra-id-auth-middleware.md) — JWT validation
- [Story 1.2.2: OBO token exchange](../proj-mgmt/epics/01-foundation/1.2-authn/1.2.2-obo-token-exchange.md) — On-Behalf-Of flow
- [Feature 1.4: External Downstream API](../proj-mgmt/epics/01-foundation/1.4-external-api/1.4-external-api.md) — external API service
- [Story 8.1.2: Entra ID OIDC sign-in](../proj-mgmt/epics/08-web-app/8.1.2-entra-id-oidc-auth/8.1.2-entra-id-oidc-auth.md) — Web app sign-in flow
- [RESEARCH: App registration architecture](../proj-mgmt/epics/01-foundation/1.2-authn/1.2.3-app-registrations-docs/RESEARCH-app-registration-architecture.md) — full research plan

---

## Architecture Overview

```
User (Browser)
  │
  ├─ 1. OIDC sign-in (Authorization Code + PKCE)
  │     → Entra ID issues tokens (audience: Web App)
  ▼
Blazor Web App  [App Reg #2: LawCorp Web App]
  │
  ├─ 2. Acquires OBO token (scope: api://<mcp-server>/access_as_user)
  ▼
MCP Server API  [App Reg #1: LawCorp MCP Server]
  │
  ├─ 3a. OBO → api://<external-api>/data.read  ──▶  External API [App Reg #3]
  ├─ 3b. OBO → https://graph.microsoft.com/...  ──▶  Microsoft Graph
  └─ 3c. Claim extraction → EF Core filters     ──▶  Local SQL Database
```

Each app registration has its own `api://` audience URI, scopes, and credentials. The MCP server acts as a **middle-tier API** that performs OBO token exchange to call downstream APIs under the user's delegated identity.

## Table of Contents

- [Authentication Configuration Guide](#authentication-configuration-guide)
  - [Architecture Overview](#architecture-overview)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Prerequisites](#prerequisites)
  - [App Registration Summary](#app-registration-summary)
  - [Part A: MCP Server Registration (Steps 1–9)](#part-a-mcp-server-registration-steps-19)
  - [Part B: Web App Registration (Step 10)](#part-b-web-app-registration-step-10)
  - [Part C: External API Registration (Step 11)](#part-c-external-api-registration-step-11)
  - [How It Works](#how-it-works)
    - [Multi-Hop Authentication Flow](#multi-hop-authentication-flow)
    - [Key Classes](#key-classes)
  - [Testing](#testing)
    - [Test with Demo Mode (No Azure Setup Required)](#test-with-demo-mode-no-azure-setup-required)
    - [Test with Entra ID](#test-with-entra-id)
    - [Verify Identity Resolution](#verify-identity-resolution)
    - [Persona Test Matrix](#persona-test-matrix)
  - [Troubleshooting](#troubleshooting)

---

## Overview

The Law-Corp MCP solution supports two authentication modes controlled by the `UseAuth` setting in `appsettings.json`:

| `UseAuth` | Mode | Identity | Transport |
|---|---|---|---|
| `false` (default) | **Demo mode** | `AnonymousUserContext` — acts as a Partner with full access | stdio (no HTTP required) |
| `true` | **Entra ID mode** | `EntraIdUserContext` — resolved from JWT claims + database | HTTP (Bearer token required) |

When auth is enabled, the MCP server:

1. Validates the inbound Entra ID JWT (issuer, audience, signature)
2. Resolves the `oid` claim to a `User` record in the local database
3. Populates `IUserContext` and `IFirmIdentityContext` with the user's identity
4. Provides `IDownstreamTokenProvider` for On-Behalf-Of (OBO) token exchange to call Microsoft Graph **and** the external API

## Prerequisites

- An Azure subscription with Microsoft Entra ID (Azure AD) tenant
- **Global Administrator** or **Application Administrator** role in the tenant
- .NET 9 SDK
- SQL Server Express (local) with the Law-Corp database seeded
- The user records in the database must have their `EntraObjectId` column populated (see [Step 7](#step-7-create-persona-seedjson-with-your-tenants-entra-id-mappings))

---

## App Registration Summary

Three Entra ID app registrations are required for the full solution:

| # | Registration | Auth Flow | Purpose | Configured In |
|---|---|---|---|---|
| 1 | **LawCorp MCP Server** | JWT Bearer + OBO | MCP resource provider; validates tokens, exchanges OBO to downstream APIs | Part A (Steps 1–9) |
| 2 | **LawCorp Web App** | OIDC Authorization Code | Blazor MCP client; user signs in, acquires token for MCP server | Part B (Step 10) |
| 3 | **LawCorp External API** | JWT Bearer (receives OBO) | Independent downstream API; validates OBO tokens from MCP server | Part C (Step 11) |

You can configure them in any order, but the scopes and permissions reference each other. We recommend following Parts A → B → C.

---

## Step 1: Create the Azure App Registration

1. Go to the [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**

2. Fill in:
   | Field | Value |
   |---|---|
   | **Name** | `LawCorp MCP Server` |
   | **Supported account types** | Accounts in this organizational directory only (single tenant) |
   | **Redirect URI** | Leave blank for now (the MCP server is a web API, not a web app) |

3. Click **Register**

4. Note down:
   - **Application (client) ID** → this is your `ClientId`
   - **Directory (tenant) ID** → this is your `TenantId`

---

## Step 2: Configure API Permissions

The MCP server needs delegated permissions for both Microsoft Graph and the external API to access downstream resources on behalf of the user.

### 2a. Microsoft Graph permissions

1. In your app registration → **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**

2. Add the following permissions:

   | Permission | Purpose |
   |---|---|
   | `User.Read` | Read the signed-in user's profile |
   | `Calendars.Read` | Read the user's calendar events (Outlook) |
   | `Calendars.ReadWrite` | Create/update calendar events |
   | `Mail.Read` | Read the user's mail |
   | `Files.Read.All` | Read files the user has access to (SharePoint/OneDrive) |
   | `Sites.Read.All` | Read SharePoint sites the user has access to |

3. Click **Grant admin consent for [your tenant]** (requires admin role)

### 2b. External API permissions

> Complete this step after creating the external API registration in [Part C (Step 11)](#step-11-configure-the-external-api). You can return here once the external API's scope exists.

1. In the MCP server app registration → **API permissions** → **Add a permission** → **My APIs** → select **LawCorp External API**
2. Select the `data.read` and `data.write` delegated permissions
3. Click **Grant admin consent**

> **Least privilege:** Add only the scopes needed for the tools you plan to enable. You can always add more later. Each tool's documentation specifies which scopes it requires.

---

## Step 3: Define App Roles

App roles map Entra ID users to the Law-Corp domain model. The MCP server reads the `roles` claim from the JWT to determine the user's `AttorneyRole`.

1. In your app registration → **App roles** → **Create app role**

2. Create the following roles:

   | Display Name | Value | Description | Allowed member types |
   |---|---|---|---|
   | Partner | `Partner` | Full case access, admin actions, billing | Users/Groups |
   | Associate | `Associate` | Assigned cases, own time entries | Users/Groups |
   | OfCounsel | `OfCounsel` | Practice group cases (read-only) | Users/Groups |
   | Paralegal | `Paralegal` | Assigned cases, no billing | Users/Groups |
   | LegalAssistant | `LegalAssistant` | Delegated attorney's cases only | Users/Groups |
   | Intern | `Intern` | Assigned cases, redacted privileged content | Users/Groups |

> The `Value` field is case-sensitive and must match the enum names exactly. The `UserContextResolutionMiddleware` reads the `roles` claim and maps it to `AttorneyRole`.

---

## Step 4: Create a Client Secret or Certificate

The server needs credentials to perform the OBO token exchange with Microsoft Entra ID.

### Option A: Client Secret (for development)

1. In your app registration → **Certificates & secrets** → **Client secrets** → **New client secret**
2. Set a description (e.g. `MCP Server Dev`) and expiration
3. Copy the **Value** immediately (it won't be shown again) → this is your `ClientSecret`

### Option B: Certificate (recommended for production)

1. Generate a self-signed certificate or use one from your PKI
2. Upload the `.cer` public key in **Certificates & secrets** → **Certificates** → **Upload certificate**
3. In `appsettings.json`, use `ClientCertificates` instead of `ClientSecret`:

```json
{
  "AzureAd": {
    "ClientCertificates": [
      {
        "SourceType": "StoreWithThumbprint",
        "CertificateStorePath": "CurrentUser/My",
        "CertificateThumbprint": "YOUR_CERT_THUMBPRINT"
      }
    ]
  }
}
```

---

## Step 5: Expose an API (Scope)

This defines the scope that client applications request when calling the MCP server API.

1. In your app registration → **Expose an API**

2. Set the **Application ID URI** to `api://<your-client-id>` (click **Set** next to the default)

3. Click **Add a scope**:
   | Field | Value |
   |---|---|
   | Scope name | `access_as_user` |
   | Who can consent | Admins and users |
   | Admin consent display name | Access Law-Corp MCP Server as user |
   | Admin consent description | Allows the app to access the Law-Corp MCP server on behalf of the signed-in user |

4. Note the full scope URI: `api://<your-client-id>/access_as_user`

### Understanding the scope URI

The scope URI has three parts:

```
api://d1234567-abcd-ef01-2345-6789abcdef01/access_as_user
└─┬─┘ └──────────────┬───────────────────┘ └──────┬──────┘
scheme   Application ID URI (MCP server's          scope name
         client ID from Step 1)
```

- **`api://`** — a scheme prefix that Entra ID uses for custom API scopes (as opposed to `https://graph.microsoft.com/` for Microsoft Graph scopes)
- **`d1234567-...`** — the **MCP server's** Application (client) ID. This is the same GUID shown on the MCP server app registration's Overview page. It is **not** the web app's client ID.
- **`access_as_user`** — the scope name you defined above. This is a delegated permission, meaning it represents the intersection of what the calling app is allowed to do and what the signed-in user is allowed to do.

This scope is used in two places:

1. **Web app `appsettings`** (`McpServer:Scopes`) — the web app requests this scope when acquiring a token to call the MCP server on behalf of the signed-in user
2. **MCP server token validation** — the server verifies that inbound tokens include this scope in the `scp` claim

> **Common mistake:** Using the web app's client ID instead of the MCP server's client ID in the scope URI. The scope is *defined on* the MCP server registration and *requested by* the web app. The client ID in the URI always refers to the API being called, not the caller.

---

## Step 6: Assign Users to App Roles

Each Entra ID user who will use the MCP server must be assigned to one of the app roles defined in Step 3.

1. Go to **Microsoft Entra ID** → **Enterprise applications** → find **LawCorp MCP Server**
2. → **Users and groups** → **Add user/group**
3. Select the user and assign them to the appropriate role

Persona-to-role mappings for the test matrix:

| Persona | App Role | Notes |
|---|---|---|
| Harvey Specter | Partner | Full access; supervises Elle and Vinny |
| Kim Wexler | Associate | Assigned cases only |
| Alan Shore | OfCounsel | Own practice group (read-only) |
| Erin Brockovich | Paralegal | Assigned cases; no billing |
| Elle Woods | LegalAssistant | Assigned to Harvey; sees Harvey's cases |
| Vinny Gambini | Intern | Assigned cases; redacted privileged content |

---

## Step 7: Create `persona-seed.json` with your tenant's Entra ID mappings

The `UserContextResolutionMiddleware` resolves the JWT's `oid` claim to a database record via the `EntraObjectId` column. The six canonical personas are seeded automatically by the `PersonaSeeder` when `SeedMockData=true` — their Entra ID mappings (email + Object ID) come from a `persona-seed.json` config file that is **gitignored** so your tenant values stay out of source control.

### 7a. Find the Entra Object IDs

For each user created in Step 6, go to **Microsoft Entra ID** → **Users** → select the user → copy the **Object ID** (a GUID).

### 7b. Create the persona seed config

Copy the example file and fill in your tenant's values:

```bash
cp src/LawCorp.Mcp.Server/persona-seed.json.example \
   src/LawCorp.Mcp.Server/persona-seed.json
```

Then edit `persona-seed.json`:

```json
{
  "PersonaSeed": {
    "HarveySpecter": {
      "Email": "harvey@yourtenant.onmicrosoft.com",
      "EntraObjectId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    },
    "KimWexler": {
      "Email": "kim@yourtenant.onmicrosoft.com",
      "EntraObjectId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy"
    },
    "AlanShore": {
      "Email": "alan@yourtenant.onmicrosoft.com",
      "EntraObjectId": "..."
    },
    "ErinBrockovich": {
      "Email": "erin@yourtenant.onmicrosoft.com",
      "EntraObjectId": "..."
    },
    "ElleWoods": {
      "Email": "elle@yourtenant.onmicrosoft.com",
      "EntraObjectId": "..."
    },
    "VinnyGambini": {
      "Email": "vinny@yourtenant.onmicrosoft.com",
      "EntraObjectId": "..."
    }
  }
}
```

> **Security:** `persona-seed.json` is listed in `.gitignore` — it will not be committed. Only `persona-seed.json.example` (with placeholder values) is tracked in source control.

### 7c. Re-seed the Database

Set `SeedMockData=true` in your `appsettings.Development.json` and restart the server. The seeder reads the `PersonaSeed` config section, creates the six personas with your Entra IDs, then generates random attorneys to fill out the firm.

If `persona-seed.json` is missing, personas are still seeded but with empty emails and no `EntraObjectId` — useful for demo mode (`UseAuth=false`) where auth isn't needed.

---

## Step 8: Update appsettings

Copy `appsettings.Development.json.example` to `appsettings.Development.json` and fill in your values:

```json
{
  "Transport": "http",
  "UseAuth": true,
  "SeedMockData": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "LawCorpDb": "Server=.\\SQLEXPRESS;Database=LawCorpLocal;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_MCP_SERVER_CLIENT_ID",
    "ClientSecret": "YOUR_MCP_SERVER_CLIENT_SECRET"
  },
  "DownstreamApis": {
    "ExternalApi": {
      "BaseUrl": "http://localhost:5002",
      "Scopes": [ "api://YOUR_EXTERNAL_API_CLIENT_ID/data.read" ]
    }
  }
}
```

### Configuration Reference

| Key | Description | Example |
|---|---|---|
| `Transport` | Transport mode: `stdio` (CLI/subprocess) or `http` (Kestrel, supports auth) | `http` |
| `UseAuth` | `true` to enable Entra ID auth; `false` for anonymous mode | `true` |
| `Kestrel:Endpoints:Http:Url` | Listen URL for HTTP transport | `http://localhost:5000` |
| `AzureAd:Instance` | Entra ID login endpoint | `https://login.microsoftonline.com/` |
| `AzureAd:TenantId` | Your Azure AD tenant ID (GUID) | `12345678-abcd-...` |
| `AzureAd:ClientId` | MCP Server Application (client) ID from Step 1 | `87654321-dcba-...` |
| `AzureAd:ClientSecret` | MCP Server client secret value from Step 4 | `abc123~secret` |
| `DownstreamApis:MicrosoftGraph:BaseUrl` | Graph API base URL | `https://graph.microsoft.com/v1.0` |
| `DownstreamApis:MicrosoftGraph:Scopes` | Graph scopes for OBO exchange | `["User.Read", "Calendars.Read", ...]` |
| `DownstreamApis:ExternalApi:BaseUrl` | External API base URL | `http://localhost:5002` |
| `DownstreamApis:ExternalApi:Scopes` | External API scopes for OBO exchange | `["api://<ext-api-id>/data.read"]` |

> **Security:** Never commit `appsettings.Development.json` to source control. It is listed in `.gitignore`. The `appsettings.json` file contains empty placeholder values that are safe to commit.

---

## Step 9: Enable Authentication

Set both `Transport` and `UseAuth` in your `appsettings.Development.json`:

```json
{
  "Transport": "http",
  "UseAuth": true
}
```

`Transport` and `UseAuth` are independent settings:

| Transport | UseAuth | Behaviour |
|---|---|---|
| `stdio` | `false` | CLI mode — AnonymousUserContext with full Partner access. Used by Claude Desktop, VS Code, Inspector (stdio). |
| `stdio` | `true` | Not yet supported — falls back to anonymous with a warning. Future: CLI token auth. |
| `http` | `false` | HTTP mode without auth — anonymous access, useful for load testing or HTTP-based Inspector usage. |
| `http` | `true` | **Full auth** — Entra ID JWT validation, OBO, user context resolution. Production path. |

When `Transport=http` and `UseAuth=true`, the server uses `WebApplication.CreateBuilder` (ASP.NET Core) and registers:

1. **JWT Bearer authentication** via `Microsoft.Identity.Web` — validates Entra ID tokens
2. **Authorization middleware** — enforces policies
3. **User context resolution** — `UserContextResolutionMiddleware` maps JWT claims to `EntraIdUserContext` and `EntraFirmIdentityContext`
4. **OBO token acquisition** — `IDownstreamTokenProvider` exchanges user tokens for Graph-scoped and external-API-scoped tokens
5. **In-memory token cache** — prevents redundant OBO exchanges within the same process

The MCP endpoint is exposed at `/mcp` via Streamable HTTP (SSE for server-to-client events). The MCP SDK packages used are `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` at version `1.0.0-rc.1`.

See [ADR-004: Dual transport Web API primary](../proj-mgmt/decisions/004-dual-transport-web-api-primary.md) for the architectural decision behind dual transport support.

---

## Step 10: Configure the Web App (Blazor)

The `LawCorp.Mcp.Web` Blazor Web App provides a browser-based UI for interacting with the MCP server. It uses OIDC (not JWT Bearer) to authenticate users — the user signs in via a browser redirect to Entra ID, and the app receives an authorization code that it exchanges for tokens server-side.

See [ADR-006](../proj-mgmt/decisions/006-web-app-architecture.md) for the architecture decision and [Story 8.1.2](../proj-mgmt/epics/08-web-app/8.1.2-entra-id-oidc-auth/8.1.2-entra-id-oidc-auth.md) for the implementation details.

### 10a. Create a separate app registration for the web app

The web app needs its own app registration (separate from the MCP server API registration created in Step 1) because it uses a different auth flow (OIDC authorization code vs. JWT Bearer).

1. Go to the [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**

2. Fill in:
   | Field | Value |
   |---|---|
   | **Name** | `LawCorp Web App` |
   | **Supported account types** | Accounts in this organizational directory only (single tenant) |
   | **Redirect URI** | **Web** — `https://localhost:5001/signin-oidc` |

3. Click **Register**

4. Note down:
   - **Application (client) ID** → this is the web app's `ClientId`
   - The **Directory (tenant) ID** is the same as the MCP server's

5. Create a **Client Secret** for the web app:
   - → **Certificates & secrets** → **Client secrets** → **New client secret**
   - Copy the **Value** → this is the web app's `ClientSecret`

6. Grant API permission to call the MCP server:
   - → **API permissions** → **Add a permission** → **My APIs** → select **LawCorp MCP Server**
   - Select the `access_as_user` delegated permission
   - Click **Grant admin consent**

7. (Optional) Add the same **App Roles** as the MCP server (Step 3) if you want role claims in the web app's token. Alternatively, the roles will be present in the MCP server's token when the web app calls it via OBO.

### 10b. Configure appsettings for the web app

Copy the example config and fill in your values:

```bash
cp src/LawCorp.Mcp.Web/appsettings.Development.json.example \
   src/LawCorp.Mcp.Web/appsettings.Development.json
```

Edit `src/LawCorp.Mcp.Web/appsettings.Development.json`:

```json
{
  "UseAuth": true,
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_WEB_APP_CLIENT_ID",
    "ClientSecret": "YOUR_WEB_APP_CLIENT_SECRET"
  },
  "McpServer": {
    "Endpoint": "http://localhost:5000/mcp",
    "Scopes": [ "api://YOUR_MCP_SERVER_CLIENT_ID/access_as_user" ]
  }
}
```

| Key | Description |
|---|---|
| `AzureAd:TenantId` | Same tenant ID as the MCP server |
| `AzureAd:ClientId` | The **web app's** Application (client) ID from Step 10a |
| `AzureAd:ClientSecret` | The **web app's** client secret from Step 10a |
| `McpServer:Scopes` | The scope exposed by the **MCP server** registration (Step 5). Format: `api://<MCP-SERVER-CLIENT-ID>/access_as_user`. The GUID is the **MCP server's** client ID — find it at Azure Portal → "LawCorp MCP Server" → Overview → Application (client) ID. Do **not** use the web app's client ID here. See [Step 5](#step-5-expose-an-api-scope) for details. |

> **Security:** `appsettings.Development.json` is gitignored. The committed `appsettings.json` contains empty placeholders. Never commit real secrets.

### 10c. Run the web app with auth

Start the MCP server in HTTP mode with auth (if you want to test end-to-end tool invocation):

```bash
dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

In a separate terminal, start the web app:

```bash
dotnet run --project src/LawCorp.Mcp.Web --launch-profile https
```

The web app launches at `https://localhost:5001`.

### 10d. Test the sign-in flow

1. Open `https://localhost:5001` in your browser
2. The home page loads with a "Sign in" button in the header and a "Not signed in" message
3. Click **Sign in** → you are redirected to the Microsoft login page
4. Enter the credentials of one of your test personas (e.g. `harvey@yourtenant.onmicrosoft.com`)
5. After authentication, you are redirected back to the app
6. The home page now shows an identity card with:
   - **Display name** (e.g. "Harvey Specter")
   - **Role** (e.g. "Partner") — from the `roles` claim
   - **Email** (e.g. "harvey@yourtenant.onmicrosoft.com") — from the `preferred_username` claim
7. The header shows the persona avatar, name, role badge, and a "Sign out" button
8. Click **Sign out** → session is cleared, you are redirected through Entra ID sign-out and back to the app

### 10e. Verify claims

After signing in, navigate to `/account/claims` (available in the left nav under **Observability** → **Claims**). This page displays all claims from your authenticated session, including:

| Claim | What to verify |
|---|---|
| `name` | Your display name |
| `preferred_username` | Your UPN / email |
| `oid` | Your Entra Object ID (should match `persona-seed.json`) |
| `roles` | Your app role (Partner, Associate, etc.) |
| `aud` | Should match the web app's Client ID |
| `tid` | Should match your Tenant ID |

If `roles` is missing, ensure the user is assigned to an app role in the web app's Enterprise Application (Step 10a.7) or that app roles are defined in the registration.

---

## Step 11: Configure the External API

The `LawCorp.Mcp.ExternalApi` project is an **independent downstream API** that the MCP server calls via OBO. It demonstrates true multi-hop identity delegation: the MCP server exchanges the user's token for an external-API-scoped token, and the external API validates that OBO token and enforces its own authorization.

See [Feature 1.4: External Downstream API](../proj-mgmt/epics/01-foundation/1.4-external-api/1.4-external-api.md) for the implementation details.

### 11a. Create the external API app registration

1. Go to the [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**

2. Fill in:
   | Field | Value |
   |---|---|
   | **Name** | `LawCorp External API` |
   | **Supported account types** | Accounts in this organizational directory only (single tenant) |
   | **Redirect URI** | Leave blank (this is a web API, not a web app) |

3. Click **Register**

4. Note down:
   - **Application (client) ID** → this is the external API's `ClientId`
   - The **Directory (tenant) ID** is the same as the MCP server's

### 11b. Expose an API (scopes)

1. In the external API registration → **Expose an API**
2. Set the **Application ID URI** to `api://<external-api-client-id>`
3. Click **Add a scope** and create:

   | Scope name | Who can consent | Admin display name | Admin description |
   |---|---|---|---|
   | `data.read` | Admins and users | Read Law-Corp external data as user | Allows the MCP server to read data on behalf of the signed-in user |
   | `data.write` | Admins and users | Write Law-Corp external data as user | Allows the MCP server to write data on behalf of the signed-in user |

4. Note the full scope URIs:
   - `api://<external-api-client-id>/data.read`
   - `api://<external-api-client-id>/data.write`

### 11c. Grant the MCP server permission to call the external API

Return to [Step 2b](#2b-external-api-permissions) and complete the permission grant now that the external API's scopes exist.

### 11d. Configure appsettings for the external API

Copy the example config and fill in your values:

```bash
cp src/LawCorp.Mcp.ExternalApi/appsettings.Development.json.example \
   src/LawCorp.Mcp.ExternalApi/appsettings.Development.json
```

Edit `src/LawCorp.Mcp.ExternalApi/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5002"
      }
    }
  },
  "ConnectionStrings": {
    "LawCorpDb": "Server=.\\SQLEXPRESS;Database=LawCorpLocal;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_EXTERNAL_API_CLIENT_ID",
    "Audience": "api://YOUR_EXTERNAL_API_CLIENT_ID"
  }
}
```

| Key | Description |
|---|---|
| `AzureAd:ClientId` | The **external API's** Application (client) ID from Step 11a |
| `AzureAd:Audience` | The **external API's** Application ID URI — `api://<external-api-client-id>` |
| `Kestrel:Endpoints:Http:Url` | Listen URL — `http://localhost:5002` (distinct from MCP server on 5000 and web app on 5001) |

> The external API does **not** need a client secret — it only validates inbound OBO tokens, it never performs its own OBO exchange.

### 11e. Run the full stack

Start all three services in separate terminals:

```bash
# Terminal 1: MCP Server (port 5000)
dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server

# Terminal 2: External API (port 5002)
dotnet run --no-launch-profile --project src/LawCorp.Mcp.ExternalApi

# Terminal 3: Web App (port 5001)
dotnet run --project src/LawCorp.Mcp.Web --launch-profile https
```

### 11f. Verify the multi-hop OBO flow

1. Sign in to the web app as Harvey Specter
2. Invoke a document management tool (e.g., search documents for a case)
3. Check the MCP server logs — you should see an OBO token acquisition for `api://<external-api-client-id>/data.read`
4. Check the external API logs — you should see the validated OBO token with Harvey's `oid` and identity claims
5. Verify that document results are scoped to Harvey's authorized cases

---

## How It Works

### Multi-Hop Authentication Flow

```
User (Browser)
  │
  ├─ 1. User signs in via OIDC (Blazor Web App)
  │     → Entra ID issues id_token + access_token (audience: Web App)
  │
  ├─ 2. Web app acquires OBO token for MCP Server
  │     (scope: api://<mcp-server-id>/access_as_user)
  │
  ├─ 3. MCP request with Authorization: Bearer <obo-token>
  │     │
  │     ▼
  │  JWT Bearer Middleware
  │     ├─ Validates issuer, audience, signature
  │     ├─ Populates HttpContext.User (ClaimsPrincipal)
  │     │
  │     ▼
  │  UserContextResolutionMiddleware
  │     ├─ Reads 'oid' claim from JWT
  │     ├─ Queries Users table: WHERE EntraObjectId = @oid
  │     ├─ Creates EntraIdUserContext (UserId, DisplayName, Role)
  │     ├─ Creates EntraFirmIdentityContext (+ PracticeGroupId, CaseAssignments)
  │     ├─ Stores both in HttpContext.Items
  │     │
  │     ▼
  │  MCP Tool Handler → MediatR Dispatch
  │     ├─ IMediator.Send(query/command)
  │     │
  │     ▼
  │  Handler resolves data source:
  │     ├─ Local DB handler → IFirmIdentityContext → EF Core query filters
  │     ├─ External API handler → IDownstreamTokenProvider.GetTokenAsync("ExternalApi")
  │     │     → OBO exchange → api://<external-api-id>/data.read
  │     │     → HTTP call to external API with OBO token
  │     └─ Graph handler → IDownstreamTokenProvider.GetTokenAsync("MicrosoftGraph")
  │           → OBO exchange → Graph scopes
  │           → Graph SDK call with OBO token
  │
  └─ 4. MCP response (scoped to the user's access)
```

### Key Classes

| Class | Project | Purpose |
|---|---|---|
| `IUserContext` | Core | Lightweight identity (UserId, Role, DisplayName) |
| `IFirmIdentityContext` | Core | Full identity with case assignments and practice group |
| `IDownstreamTokenProvider` | Core | Acquires OBO tokens for downstream APIs (Graph + External API) |
| `AnonymousUserContext` | Server | Demo mode — Partner with full access |
| `EntraIdUserContext` | Server | Auth mode — resolved from JWT + database |
| `EntraFirmIdentityContext` | Server | Auth mode — full identity with case assignments |
| `OboDownstreamTokenProvider` | Server | MSAL OBO exchange for downstream APIs |
| `UserContextResolutionMiddleware` | Server | Resolves identity from JWT claims + database |
| `AuthServiceCollectionExtensions` | Server | DI registration for all auth services |
| `LoginDisplay.razor` | Web | Sign-in/sign-out button and identity display in the header |
| `Claims.razor` | Web | Debug page showing all JWT claims at `/account/claims` |

---

## Testing

### Test with Demo Mode (No Azure Setup Required)

```json
{ "UseAuth": false }
```

All requests use `AnonymousUserContext` (Partner role, full access). No Azure configuration needed.

### Test with Entra ID

1. Complete Steps 1–9 above
2. Acquire a token for your app using the Azure CLI:

```bash
# Login to Azure
az login

# Acquire a token for the MCP server API
az account get-access-token \
  --resource api://YOUR_CLIENT_ID \
  --query accessToken -o tsv
```

3. Use the token in an HTTP request to the MCP server (requires HTTP transport):

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Authorization: Bearer <your-token>" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"SearchCases","arguments":{"query":"merger"}}}'
```

### Verify Identity Resolution

Check the server logs for identity resolution messages:

```
info: LawCorp.Mcp.Server.Auth.UserContextResolutionMiddleware
      Resolved user: Harvey Specter (Partner) from oid: xxxxxxxx-...
```

### Persona Test Matrix

Verify that each persona sees only the data they're authorized to access:

| Persona | Role | Expected Access |
|---|---|---|
| Harvey Specter | Partner | All M&A cases, full billing, all documents, full Graph scopes |
| Kim Wexler | Associate | Assigned cases only, own time entries, own mailbox/calendar |
| Alan Shore | OfCounsel | Own practice group cases (read-only), own mailbox/calendar |
| Erin Brockovich | Paralegal | Assigned cases, no billing, limited Graph scopes |
| Elle Woods | LegalAssistant | Harvey's cases only, Harvey's calendar (delegated) |
| Vinny Gambini | Intern | Assigned cases, redacted privileged content, own calendar only |

---

## Troubleshooting

### "Authenticated user context was not resolved"

**Cause:** The JWT's `oid` claim doesn't match any `Attorney.EntraObjectId` in the database.

**Fix:** Verify the Entra Object ID mapping (Step 7). Run:

```sql
SELECT Id, FirstName, LastName, EntraObjectId FROM Attorneys;
```

### "AADSTS65001: The user has not consented"

**Cause:** Admin consent hasn't been granted for the required Graph permissions.

**Fix:** Go to the app registration → API permissions → Grant admin consent.

### "AADSTS700016: Application not found in the directory"

**Cause:** The `ClientId` in `appsettings.json` doesn't match the app registration.

**Fix:** Verify `AzureAd:ClientId` matches the Application (client) ID in the Azure Portal.

### "Bearer error=invalid_token"

**Cause:** The token's audience doesn't match the server's expected audience.

**Fix:** Ensure the token was acquired with the scope `api://<your-client-id>/access_as_user` and that `AzureAd:ClientId` is correct.

### Token caching not working

**Cause:** Each request is performing a fresh OBO exchange.

**Fix:** The default in-memory token cache is configured via `AddInMemoryTokenCaches()`. For distributed deployments, configure a distributed cache (Redis, SQL Server) in `AuthServiceCollectionExtensions`.
