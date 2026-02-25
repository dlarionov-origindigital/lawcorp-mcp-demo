# Authentication Configuration Guide

This guide walks through configuring Microsoft Entra ID authentication for the Law-Corp MCP server. When complete, every MCP tool call will execute under the calling user's identity — the server only sees and modifies data the user is personally authorized to access.

**Related:**
- [ADR-005: OAuth identity passthrough](../proj-mgmt/decisions/005-oauth-identity-passthrough.md) — architectural rationale
- [ADR-006: Web app architecture](../proj-mgmt/decisions/006-web-app-architecture.md) — Blazor web app decision
- [Story 1.2.4: Downstream resource access](../proj-mgmt/epics/01-foundation/1.2.4-downstream-resource-access/1.2.4-downstream-resource-access.md) — user story
- [Story 1.2.1: Entra ID auth middleware](../proj-mgmt/epics/01-foundation/1.2.1-entra-id-auth-middleware.md) — JWT validation
- [Story 1.2.2: OBO token exchange](../proj-mgmt/epics/01-foundation/1.2.2-obo-token-exchange.md) — On-Behalf-Of flow
- [Story 8.1.2: Entra ID OIDC sign-in](../proj-mgmt/epics/08-web-app/8.1.2-entra-id-oidc-auth/8.1.2-entra-id-oidc-auth.md) — Web app sign-in flow

---

## Table of Contents

- [Authentication Configuration Guide](#authentication-configuration-guide)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Prerequisites](#prerequisites)
  - [Step 1: Create the Azure App Registration](#step-1-create-the-azure-app-registration)
  - [Step 2: Configure API Permissions](#step-2-configure-api-permissions)
  - [Step 3: Define App Roles](#step-3-define-app-roles)
  - [Step 4: Create a Client Secret or Certificate](#step-4-create-a-client-secret-or-certificate)
    - [Option A: Client Secret (for development)](#option-a-client-secret-for-development)
    - [Option B: Certificate (recommended for production)](#option-b-certificate-recommended-for-production)
  - [Step 5: Expose an API (Scope)](#step-5-expose-an-api-scope)
  - [Step 6: Assign Users to App Roles](#step-6-assign-users-to-app-roles)
  - [Step 7: Map Entra Object IDs to Attorneys in the Database](#step-7-map-entra-object-ids-to-attorneys-in-the-database)
    - [Find the Entra Object ID](#find-the-entra-object-id)
    - [Update the Database](#update-the-database)
  - [Step 8: Update appsettings](#step-8-update-appsettings)
    - [Configuration Reference](#configuration-reference)
  - [Step 9: Enable Authentication](#step-9-enable-authentication)
    - [Transport Requirement](#transport-requirement)
  - [Step 10: Configure the Web App (Blazor)](#step-10-configure-the-web-app-blazor)
    - [10a. Create a separate app registration for the web app](#10a-create-a-separate-app-registration-for-the-web-app)
    - [10b. Configure appsettings for the web app](#10b-configure-appsettings-for-the-web-app)
    - [10c. Run the web app with auth](#10c-run-the-web-app-with-auth)
    - [10d. Test the sign-in flow](#10d-test-the-sign-in-flow)
    - [10e. Verify claims](#10e-verify-claims)
  - [How It Works](#how-it-works)
    - [Authentication Flow](#authentication-flow)
    - [Key Classes](#key-classes)
  - [Testing](#testing)
    - [Test with Demo Mode (No Azure Setup Required)](#test-with-demo-mode-no-azure-setup-required)
    - [Test with Entra ID](#test-with-entra-id)
    - [Verify Identity Resolution](#verify-identity-resolution)
    - [Persona Test Matrix](#persona-test-matrix)
  - [Troubleshooting](#troubleshooting)
    - ["Authenticated user context was not resolved"](#authenticated-user-context-was-not-resolved)
    - ["AADSTS65001: The user has not consented"](#aadsts65001-the-user-has-not-consented)
    - ["AADSTS700016: Application not found in the directory"](#aadsts700016-application-not-found-in-the-directory)
    - ["Bearer error=invalid\_token"](#bearer-errorinvalid_token)
    - [Token caching not working](#token-caching-not-working)

---

## Overview

The Law-Corp MCP server supports two authentication modes controlled by the `UseAuth` setting in `appsettings.json`:

| `UseAuth` | Mode | Identity | Transport |
|---|---|---|---|
| `false` (default) | **Demo mode** | `AnonymousUserContext` — acts as a Partner with full access | stdio (no HTTP required) |
| `true` | **Entra ID mode** | `EntraIdUserContext` — resolved from JWT claims + database | HTTP (Bearer token required) |

When auth is enabled, the server:

1. Validates the inbound Entra ID JWT (issuer, audience, signature)
2. Resolves the `oid` claim to an `Attorney` record in the local database
3. Populates `IUserContext` and `IFirmIdentityContext` with the attorney's identity
4. Provides `IDownstreamTokenProvider` for On-Behalf-Of (OBO) token exchange to call Microsoft Graph

## Prerequisites

- An Azure subscription with Microsoft Entra ID (Azure AD) tenant
- **Global Administrator** or **Application Administrator** role in the tenant
- .NET 9 SDK
- SQL Server Express (local) with the Law-Corp database seeded
- The attorney records in the database must have their `EntraObjectId` column populated (see [Step 7](#step-7-map-entra-object-ids-to-attorneys-in-the-database))

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

The MCP server needs delegated permissions for Microsoft Graph to access SharePoint, Outlook Calendar, and Mail on behalf of the user.

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
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
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
| `AzureAd:ClientId` | Application (client) ID from Step 1 | `87654321-dcba-...` |
| `AzureAd:ClientSecret` | Client secret value from Step 4 | `abc123~secret` |
| `DownstreamApis:MicrosoftGraph:BaseUrl` | Graph API base URL | `https://graph.microsoft.com/v1.0` |
| `DownstreamApis:MicrosoftGraph:Scopes` | Scopes for OBO exchange | `["User.Read", "Calendars.Read", ...]` |

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
4. **OBO token acquisition** — `IDownstreamTokenProvider` exchanges user tokens for Graph-scoped tokens
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

## How It Works

### Authentication Flow

```
Client (Claude Desktop, Foundry, etc.)
  │
  ├─ 1. User authenticates with Entra ID
  ├─ 2. Client receives access token (audience: api://<ClientId>)
  │
  ├─ 3. MCP request with Authorization: Bearer <token>
  │     │
  │     ▼
  │  JWT Bearer Middleware
  │     ├─ Validates issuer, audience, signature
  │     ├─ Populates HttpContext.User (ClaimsPrincipal)
  │     │
  │     ▼
  │  UserContextResolutionMiddleware
  │     ├─ Reads 'oid' claim from JWT
  │     ├─ Queries Attorney table: WHERE EntraObjectId = @oid
  │     ├─ Creates EntraIdUserContext (UserId, DisplayName, Role)
  │     ├─ Creates EntraFirmIdentityContext (+ PracticeGroupId, CaseAssignments)
  │     ├─ Stores both in HttpContext.Items
  │     │
  │     ▼
  │  MCP Tool Handler (e.g. SearchCases)
  │     ├─ Injects IUserContext → gets the authenticated attorney's identity
  │     ├─ Injects IFirmIdentityContext → gets case assignments, practice group
  │     ├─ Injects IDownstreamTokenProvider → OBO exchange for Graph calls
  │     │
  │     ▼
  │  Downstream Resources
  │     ├─ Microsoft Graph: OBO token → User.Read, Calendars.Read, etc.
  │     └─ Local Database: IFirmIdentityContext → EF Core query filters
  │
  └─ 4. MCP response (scoped to the user's access)
```

### Key Classes

| Class | Project | Purpose |
|---|---|---|
| `IUserContext` | Core | Lightweight identity (UserId, Role, DisplayName) |
| `IFirmIdentityContext` | Core | Full identity with case assignments and practice group |
| `IDownstreamTokenProvider` | Core | Acquires OBO tokens for downstream APIs |
| `AnonymousUserContext` | Server | Demo mode — Partner with full access |
| `EntraIdUserContext` | Server | Auth mode — resolved from JWT + database |
| `EntraFirmIdentityContext` | Server | Auth mode — full identity with case assignments |
| `OboDownstreamTokenProvider` | Server | MSAL OBO exchange for Microsoft Graph |
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
