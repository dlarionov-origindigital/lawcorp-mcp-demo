# Testing Authentication with MCP Inspector

This guide walks through verifying Entra ID authentication locally using real Azure AD personas and the MCP Inspector. By the end you will have logged in as two different personas and confirmed that each sees only the data they are authorized to access.

**Prerequisites — complete these first:**
- [docs/auth-config.md](./auth-config.md) — Azure app registration, user creation, appsettings
- [src/LawCorp.Mcp.MockData/Personas/README.md](../src/LawCorp.Mcp.MockData/Personas/README.md) — Persona seed data with your tenant's OIDs
- [docs/local-dev.md](./local-dev.md) — Local dev environment, SQL Express, Node.js

**Related:**
- [ADR-005: OAuth identity passthrough](../proj-mgmt/decisions/005-oauth-identity-passthrough.md)
- [Story 7.5.3: Manual E2E auth verification](../proj-mgmt/epics/07-testing/7.5.3-manual-e2e-auth-verification.md)
- [Story 8.1.2: Entra ID OIDC sign-in](../proj-mgmt/epics/08-web-app/8.1.2-entra-id-oidc-auth/8.1.2-entra-id-oidc-auth.md) — Web app sign-in flow
- [Web app README](../src/LawCorp.Mcp.Web/README.md) — alternative to Inspector for testing auth
- [MCP Inspector — Official Docs](https://modelcontextprotocol.io/docs/tools/inspector)
- [Microsoft identity platform — OAuth 2.0 On-Behalf-Of flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)

---

## Table of Contents

1. [Overview](#overview)
2. [Step 1: Verify persona seed data in the database](#step-1-verify-persona-seed-data-in-the-database)
3. [Step 2: Enable HTTP transport](#step-2-enable-http-transport)
4. [Step 3: Start the server with auth enabled](#step-3-start-the-server-with-auth-enabled)
5. [Step 4: Acquire a token for a persona](#step-4-acquire-a-token-for-a-persona)
6. [Step 5: Connect MCP Inspector with auth](#step-5-connect-mcp-inspector-with-auth)
7. [Step 6: Verify access as Harvey Specter (Partner)](#step-6-verify-access-as-harvey-specter-partner)
8. [Step 7: Switch to Kim Wexler (Associate) and compare](#step-7-switch-to-kim-wexler-associate-and-compare)
9. [Full persona verification matrix](#full-persona-verification-matrix)
10. [Troubleshooting](#troubleshooting)
11. [References](#references)

---

## Overview

The auth testing workflow has three stages:

```
┌─────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│  1. Verify   │    │  2. Acquire       │    │  3. Test via MCP     │
│  DB seeding  │ →  │  persona tokens   │ →  │  Inspector (HTTP)    │
│  (SQL query) │    │  (az cli / MSAL)  │    │  with Bearer token   │
└─────────────┘    └──────────────────┘    └─────────────────────┘
```

Stage 1 works immediately with the current stdio server. Stages 2–3 require HTTP transport, which involves upgrading the MCP SDK and switching `Program.cs` to use `WebApplication` when `UseAuth=true`.

---

## Step 1: Verify persona seed data in the database

Before testing auth, confirm the six personas were seeded with their Entra Object IDs.

### 1a. Seed the database

Ensure `SeedMockData=true` in your `appsettings.Development.json`, then run:

```bash
dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

The server will recreate the database and seed all data (including personas) on startup. You can stop it after seeding completes (the `info: Microsoft.Hosting.Lifetime` log line appears).

### 1b. Query the persona data

Connect to your local SQL Express instance and run:

```sql
-- Verify attorney personas (Harvey, Kim, Alan)
SELECT Id, FirstName, LastName, Email, Role, EntraObjectId, PracticeGroupId
FROM Attorneys
WHERE EntraObjectId IS NOT NULL
ORDER BY Id;

-- Verify staff personas (Erin, Elle, Vinny)
SELECT 'Paralegal' AS Type, Id, FirstName, LastName, Email, EntraObjectId
FROM Paralegals WHERE EntraObjectId IS NOT NULL
UNION ALL
SELECT 'LegalAssistant', Id, FirstName, LastName, Email, EntraObjectId
FROM LegalAssistants WHERE EntraObjectId IS NOT NULL
UNION ALL
SELECT 'Intern', Id, FirstName, LastName, Email, EntraObjectId
FROM Interns WHERE EntraObjectId IS NOT NULL;
```

**Expected result — attorneys:**

| Id | FirstName | LastName | Role | EntraObjectId |
|---|---|---|---|---|
| 1 | Harvey | Specter | Partner | `c93b9c61-...` |
| 2 | Kim | Wexler | Associate | `21c1193a-...` |
| 3 | Alan | Shore | OfCounsel | `14f5bcdf-...` |

The IDs should be 1, 2, 3 because personas are seeded first. The EntraObjectIds should match the values in your `PersonaDefinitions.cs`.

If the EntraObjectId column is `NULL`, you need to update `PersonaDefinitions.cs` with your tenant's OIDs and re-seed. See [Personas/README.md](../src/LawCorp.Mcp.MockData/Personas/README.md).

---

## Step 2: Enable HTTP transport

Auth requires HTTP transport because the Bearer token arrives as an HTTP header. The current server uses stdio. This step upgrades the MCP SDK and adds HTTP/SSE support.

### 2a. Upgrade the MCP SDK

The `ModelContextProtocol.AspNetCore` package provides HTTP transport integration for ASP.NET Core:

```bash
cd src/LawCorp.Mcp.Server

# Upgrade the core package
dotnet add package ModelContextProtocol --prerelease

# Add the ASP.NET Core transport package
dotnet add package ModelContextProtocol.AspNetCore --prerelease
```

> **Note:** The ASP.NET Core package requires the MCP SDK `1.0.0-rc.1` or later. Upgrading from `0.9.0-preview.2` may require minor API adjustments if tool registration APIs changed. Check the [MCP C# SDK changelog](https://github.com/modelcontextprotocol/csharp-sdk/releases) for breaking changes.

### 2b. Update Program.cs for dual transport

Modify `Program.cs` to use `WebApplication` when auth is enabled and `Host` when it is not:

```csharp
// When UseAuth=true, use WebApplication for HTTP transport + auth middleware
// When UseAuth=false, keep the current Generic Host + stdio transport

var useAuth = /* read from config before builder creation */;

if (useAuth)
{
    var builder = WebApplication.CreateBuilder(args);

    // Database, logging (same as current)
    builder.Services.AddLawCorpDatabase(connectionString);
    builder.Services.AddEntraIdAuthentication(builder.Configuration);

    // MCP server with HTTP transport
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<UserContextResolutionMiddleware>();
    app.MapMcp();

    // Seed if configured (same as current)

    await app.RunAsync();
}
else
{
    // Current stdio path — unchanged
    var builder = Host.CreateApplicationBuilder(args);
    // ... existing code ...
}
```

The key changes:
- `WebApplication.CreateBuilder()` replaces `Host.CreateApplicationBuilder()` for the auth path
- `.WithHttpTransport()` replaces `.WithStdioServerTransport()`
- `app.MapMcp()` exposes the MCP endpoint at `/mcp`
- Auth middleware runs before MCP handlers: `UseAuthentication()` → `UseAuthorization()` → `UserContextResolutionMiddleware` → MCP

### 2c. Configure the server URL

Add the server URL to `appsettings.Development.json`:

```json
{
  "UseAuth": true,
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

---

## Step 3: Start the server with auth enabled

```bash
dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
```

You should see:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

Verify the MCP endpoint responds:

```bash
curl -s http://localhost:5000/mcp -H "Content-Type: application/json" -d '{}' -w "\n%{http_code}"
```

You should get `401` (Unauthorized) — this confirms auth middleware is active and rejecting unauthenticated requests.

---

## Step 4: Acquire a token for a persona

You need an access token scoped to your MCP server's API. There are several ways to acquire one.

### Option A: Azure CLI (simplest)

Log in as the persona user and request a token:

```bash
# Log in as Harvey Specter
az login --username harvey@yourtenant.onmicrosoft.com --password 'Welcom123@!' --tenant YOUR_TENANT_ID

# Acquire a token for the MCP server API
az account get-access-token \
  --resource api://YOUR_CLIENT_ID \
  --query accessToken -o tsv
```

Save the token — it's valid for ~60 minutes.

> **Security note:** The `az login` with `--password` is for local dev testing only. In production, use interactive login or managed identity. See [Azure CLI authentication](https://learn.microsoft.com/en-us/cli/azure/authenticate-azure-cli).

### Option B: ROPC flow via curl (non-interactive, dev only)

The Resource Owner Password Credentials (ROPC) flow is useful for scripted testing but is [not recommended for production](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth-ropc). It must be enabled in your tenant.

```bash
# Acquire token for Harvey Specter via ROPC
curl -s -X POST \
  "https://login.microsoftonline.com/YOUR_TENANT_ID/oauth2/v2.0/token" \
  -d "client_id=YOUR_CLIENT_ID" \
  -d "scope=api://YOUR_CLIENT_ID/.default" \
  -d "username=harvey@yourtenant.onmicrosoft.com" \
  -d "password=Welcom123@!" \
  -d "grant_type=password" \
  | jq -r '.access_token'
```

NOTE: Entra may have you complete a password reset in which case you can add an 'e' i.e. 'Welcom123@!' => 'Welcom123e@!'.  

> **ROPC prerequisites:** Your tenant must allow ROPC (Entra ID → Authentication → Advanced settings → "Allow public client flows" = Yes). ROPC does not work with MFA-enabled accounts. See [Microsoft ROPC documentation](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth-ropc).

### Option C: Interactive browser flow

For tenants with MFA or conditional access, use the device code flow:

```bash
# Start device code flow
az login --use-device-code --tenant YOUR_TENANT_ID

# Then acquire the token
az account get-access-token --resource api://YOUR_CLIENT_ID --query accessToken -o tsv
```

### Inspect the token (optional)

Paste the token at [jwt.ms](https://jwt.ms) to verify:
- `aud` matches `api://YOUR_CLIENT_ID`
- `oid` matches Harvey's EntraObjectId in `PersonaDefinitions.cs`
- `roles` contains `Partner`
- `name` shows the user's display name

---

## Step 5: Connect MCP Inspector with auth

### 5a. Launch the Inspector

```bash
npx @modelcontextprotocol/inspector
```

This opens the Inspector UI at `http://localhost:6274` (or similar — check the terminal output).

### 5b. Configure transport

In the Inspector UI:

1. **Transport Type:** Select **Streamable HTTP** (or **SSE** if Streamable HTTP is unavailable)
2. **URL:** Enter `http://localhost:5000/mcp`
3. **Authentication:** In the Bearer Token / Authorization field, paste the token from Step 4

> **Inspector version note:** Ensure you are on MCP Inspector v0.17.0 or later. Earlier versions had a bug where the Authorization header sent `Bearer` without the actual token ([issue #826](https://github.com/modelcontextprotocol/inspector/issues/826)). Run `npx @modelcontextprotocol/inspector@latest` to get the latest.

### 5c. Connect

Click **Connect**. The Inspector should successfully negotiate capabilities with the server. You should see the tools list populate in the Tools tab.

If connection fails, check [Troubleshooting](#troubleshooting).

---

## Step 6: Verify access as Harvey Specter (Partner)

With Harvey's token active in the Inspector:

### Test 1: Search cases

In the **Tools** tab, invoke `cases_search`:

```json
{ "query": "merger" }
```

**Expected:** Harvey (Partner, M&A practice group) should see all M&A cases. His results include cases from the full M&A practice group.

### Test 2: List case assignments

Invoke the case assignments tool for a case Harvey leads.

**Expected:** Full assignment details returned, including all attorneys on the case.

### Test 3: Access billing data

If a billing tool is available, invoke it.

**Expected:** Harvey has full billing access — all invoices and time entries should be visible.

Record the number of cases, documents, and billing records returned. You will compare these against Kim's results in the next step.

---

## Step 7: Switch to Kim Wexler (Associate) and compare

### 7a. Acquire Kim's token

```bash
# Log out of Harvey's session
az logout

# Log in as Kim Wexler
az login --username Kim@yourtenant.onmicrosoft.com --password 'Welcom123@!' --tenant YOUR_TENANT_ID

# Acquire token
az account get-access-token --resource api://YOUR_CLIENT_ID --query accessToken -o tsv
```

### 7b. Update the Inspector

1. In the Inspector, update the Bearer Token field with Kim's token
2. Click **Reconnect** (or disconnect and reconnect)

### 7c. Compare results

Invoke the same tools as Step 6:

| Tool | Harvey (Partner) | Kim (Associate) | Difference |
|---|---|---|---|
| `cases_search` | All M&A cases | Only assigned cases | Kim sees fewer cases |
| Billing data | Full access | Own time entries only | Kim cannot see invoices |
| Documents | All, including privileged | Assigned cases only | Kim cannot see unassigned docs |

**This is the core verification:** the same MCP server, the same tools, but different results based on the authenticated identity. If Kim sees the same data as Harvey, the identity passthrough is not working correctly.

---

## Full persona verification matrix

Repeat Steps 4–7 for each persona to validate the complete access model:

| Persona | Token acquisition | cases_search expected | Billing expected | Special check |
|---|---|---|---|---|
| Harvey Specter (Partner) | `az login` as Harvey | All M&A cases | Full access | Can see privileged documents |
| Kim Wexler (Associate) | `az login` as Kim | Assigned cases only | Own time entries | Cannot see unassigned cases |
| Alan Shore (OfCounsel) | `az login` as Alan | Securities cases (read-only) | Own time entries | Cannot see M&A cases |
| Erin Brockovich (Paralegal) | `az login` as Erin | Assigned cases | No billing access | Limited Graph scopes |
| Elle Woods (LegalAssistant) | `az login` as Elle | Harvey's cases only | View Harvey's entries | Sees Harvey's calendar |
| Vinny Gambini (Intern) | `az login` as Vinny | Assigned cases, redacted | No billing access | Privileged content is redacted |

See [Feature 7.2: Persona Fixture](../proj-mgmt/epics/07-testing/7.2-persona-fixture.md) for the full downstream access scope table.

---

## Troubleshooting

### "401 Unauthorized" on every request

| Possible cause | Fix |
|---|---|
| Token expired (60-min lifetime) | Re-acquire the token |
| Wrong audience (`aud` claim) | Ensure the token was acquired with `--resource api://YOUR_CLIENT_ID` |
| `AzureAd:ClientId` mismatch | Verify `appsettings.Development.json` matches your app registration |
| Token not being sent | Upgrade MCP Inspector to v0.17.0+ and verify the Bearer Token field |

### "Authenticated user context was not resolved"

The token is valid (no 401) but the `oid` doesn't match any `Attorney.EntraObjectId`.

1. Inspect the token at [jwt.ms](https://jwt.ms) — note the `oid` value
2. Query the database: `SELECT * FROM Attorneys WHERE EntraObjectId = '<oid>'`
3. If no match, update `PersonaDefinitions.cs` with the correct OID and re-seed

### Inspector shows "connection failed"

| Possible cause | Fix |
|---|---|
| Server not running | Start the server and confirm it's listening on `http://localhost:5000` |
| Wrong transport type | Select Streamable HTTP (not stdio) in the Inspector |
| Wrong URL | The MCP endpoint is at `/mcp`, not the root `/` |
| CORS issue | Add CORS policy in Program.cs if Inspector runs on a different origin |

### Harvey and Kim see the same data

Identity passthrough is not working. Check:

1. Are both tokens different? Compare the `oid` claim at [jwt.ms](https://jwt.ms)
2. Is `UseAuth=true` in appsettings? If false, `AnonymousUserContext` (Partner) is used for all requests
3. Is `UserContextResolutionMiddleware` in the pipeline? Check Program.cs
4. Do both attorneys have different `PracticeGroupId` and `CaseAssignment` records?

### ROPC flow returns "AADSTS50126: Invalid username or password"

- Verify the username is the full UPN (e.g. `harvey@yourtenant.onmicrosoft.com`)
- Verify the password is correct
- Ensure "Allow public client flows" is enabled in the app registration → Authentication → Advanced settings
- ROPC does not work with accounts that have MFA enabled

---

## Alternative: Test via the Web App (Blazor)

Instead of acquiring tokens manually and pasting them into the MCP Inspector, you can use the `LawCorp.Mcp.Web` Blazor web app for a more integrated testing experience. The web app provides:

- **Browser-based sign-in** — click "Sign in", authenticate at Entra ID, done
- **Identity verification** — the home page shows your display name, role, and email
- **Claims inspection** — navigate to `/account/claims` to see all JWT claims without needing [jwt.ms](https://jwt.ms)
- **Persona switching** — sign out and sign in as a different persona to compare access

### Quick start

1. Complete [Step 10 in auth-config.md](./auth-config.md#step-10-configure-the-web-app-blazor) to set up the web app's app registration and `appsettings.Development.json`

2. Start the MCP server:
   ```bash
   dotnet run --no-launch-profile --project src/LawCorp.Mcp.Server
   ```

3. Start the web app:
   ```bash
   dotnet run --project src/LawCorp.Mcp.Web --launch-profile https
   ```

4. Open `https://localhost:5001` and sign in as a persona

5. Navigate to `/account/claims` to verify the token contains the expected `oid`, `roles`, and `preferred_username`

6. Sign out, then sign in as a different persona to compare

> **Note:** The web app's Tool/Resource/Prompt invocation pages are placeholders (Feature 8.2). For now, use the web app for identity verification and the MCP Inspector for tool testing.

See [`src/LawCorp.Mcp.Web/README.md`](../src/LawCorp.Mcp.Web/README.md) for full web app documentation.

---

## References

- [MCP Inspector — Official Docs](https://modelcontextprotocol.io/docs/tools/inspector)
- [MCP Inspector — GitHub](https://github.com/modelcontextprotocol/inspector)
- [Microsoft identity platform — OAuth 2.0 ROPC flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth-ropc)
- [Microsoft identity platform — OAuth 2.0 On-Behalf-Of flow](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-on-behalf-of-flow)
- [Azure CLI — `az account get-access-token`](https://learn.microsoft.com/en-us/cli/azure/account#az-account-get-access-token)
- [jwt.ms — Token decoder](https://jwt.ms)
- [ModelContextProtocol.AspNetCore — C# SDK API](https://modelcontextprotocol.github.io/csharp-sdk/api/Microsoft.Extensions.DependencyInjection.HttpMcpServerBuilderExtensions.html)
- [Azure App Service — MCP server tutorial (.NET)](https://learn.microsoft.com/en-us/azure/app-service/tutorial-ai-model-context-protocol-server-dotnet)
