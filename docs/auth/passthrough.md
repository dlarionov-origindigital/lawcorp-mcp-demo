# [Passthrough](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/mcp-authentication?view=foundry)

Here’s the article, boiled down to the parts that matter for *your* goal: **“my MCP server should only be able to do what the logged-in user can do.”**

## What the article is really saying

### 1) There are only two real auth modes

**Shared authentication (no per-user context)**

* Everyone using the agent shares the same identity/secret.
* Good for: service accounts, shared API keys, “team bot” behavior.
* Bad for your goal: *you lose “act as the user.”*

**Individual authentication (per-user context persists)**

* Each user authorizes with their own account.
* This is what you want.
* In Foundry, this is called **OAuth identity passthrough**.

### 2) Foundry supports 4+ ways to connect, but only one preserves user identity

The article lists these methods and explicitly calls out whether user context persists:

* **Key-based** (API key / PAT) → **No user context**
* **Entra auth (agent identity)** → **No user context** (acts as the agent)
* **Entra auth (project managed identity)** → **No user context** (acts as the project)
* **OAuth identity passthrough** → **Yes user context persists**
* **Unauthenticated** → **No user context**

So for “calendar as them,” “mail as them,” “files as them,” etc. the recommended method is:

✅ **OAuth identity passthrough** (individual auth)

### 3) OAuth passthrough flow is “consent link → user signs in → retry”

When the agent first needs a tool that requires user consent, Foundry returns an output item like:

* `oauth_consent_request`
* contains a `consent_link`

Your app is expected to **surface that link** to the user. After they consent, you submit another request using `previous_response_id` to continue.

After the first consent, users typically **aren’t prompted again** (unless refresh tokens are revoked / expire in a way that can’t be refreshed).

### 4) It warns you not to store user-specific secrets as “project connection”

Project connections are accessible by people with project access, so the article stresses:

* Store only **shared** secrets there.
* For **user-specific** access, use **OAuth identity passthrough**.

### 5) Hosting notes for MCP servers

Foundry Agent Service only connects to **remote** MCP servers.
If you built a local MCP server, host it on:

* **Azure Container Apps** (HTTP POST/GET endpoints; rebuild container to update)
* **Azure Functions** (must support streamable HTTP / chunked/SSE style; stateless)

And regardless of host: **assume stateless**, store session state externally if needed.

---

## How this translates to *your .NET MCP server design*

### The correct mental model

Foundry is basically saying:

> “Don’t have your MCP server invent its own login system. Let Foundry handle user consent + token storage, and your MCP server just receives valid tokens and enforces them.”

So your .NET MCP server should be built like a normal protected web API:

* **Accept OAuth access tokens** (Authorization: Bearer …)
* Validate them
* Use them (or exchange them) to call downstream APIs like Microsoft Graph

### Recommended architecture for “Graph as the user”

You have two common patterns, depending on what token you receive from Foundry.

#### Pattern A: Direct user token to Graph (best case)

If the inbound token your MCP server receives is already scoped for Graph (or for the specific Microsoft service MCP target), your server can:

* Validate token
* Call Graph directly with it (Bearer passthrough)
* Enforce least privilege via scopes/roles

This is the simplest.

#### Pattern B: On-Behalf-Of (OBO) exchange (very common)

If the inbound token is “for your MCP server API” (audience = your API), then your server uses **OBO** to get a Graph token:

* Incoming user token proves identity and consent context
* Your server exchanges it for a Graph access token
* You call Graph using the exchanged token

This is the standard “API calls downstream API as user” approach in Entra.

In .NET, this usually means:

* Validate JWT (issuer/audience/signature)
* Use Microsoft identity libraries to acquire token for Graph **on behalf of** the user
* Call Graph

Auth should be exclusively:

* `Authorization: Bearer <token>`
* plus your own authorization rules per tool

---

## The“calendar setup implied by the article

For Microsoft services that require user-level permissions:

1. In Foundry, configure the MCP tool connection with **OAuth identity passthrough**

2. Use either:

   * **Managed OAuth** (publisher-managed)
   * **Custom OAuth** (you register your own Entra app)

3. If custom OAuth:

   * you configure auth/token/refresh URLs
   * you add the Foundry-provided redirect URL to your Entra app
   * you set scopes/permissions to the minimum required for those tools

Then:

* First time the user triggers calendar/mail tool usage → consent link appears → user consents
* Subsequent tool calls execute with their identity

That’s exactly the behavior you want: **“just like it was them using Outlook/Calendar.”**

---

## The one design decision you should make up front

If your MCP server will call Microsoft Graph “as user,” you want:

✅ **OAuth identity passthrough** (user context persists)

Everything else (agent identity / project MI / key-based) is explicitly “shared identity,” and doesn’t meet your stated requirement.
