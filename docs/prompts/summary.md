In MCP, **“prompts” are a first-class, discoverable capability**: the server publishes a catalog of reusable, parameterized *prompt templates/workflows*, and MCP clients (like an agent host) can surface them as selectable “templated messages” (think: slash-commands, recipe cards, workflow buttons).

## How MCP reveals available prompts

### 1) Capability discovery (server advertises it supports prompts)

In the MCP spec, “Prompts” are one of the core server features alongside Tools and Resources. ([Model Context Protocol][1])

So at a high level:

* Server: “I support `prompts/*` methods.”
* Client: “Cool—then I’ll call `prompts/list` and show the user what’s available.”

### 2) `prompts/list` (catalog / registry)

Clients discover prompt templates via the JSON-RPC method **`prompts/list`**. ([Model Context Protocol][2])

Typical response entries include:

* `name` (stable identifier)
* `title` (friendly label)
* `description` (what it does)
* `arguments` (parameters the user/client can fill in)

That’s the “reveal” step: it’s literally a **server-side prompt registry** the client can enumerate and render in UI.

### 3) `prompts/get` (materialize a prompt with arguments → message sequence)

After a user selects a template, the client calls **`prompts/get`** (same “Prompts” spec section) to retrieve the *actual prompt contents* and/or message sequence, optionally passing arguments to customize it. ([Model Context Protocol][3])

A prompt commonly resolves to a structured “message plan”, e.g.:

* system message
* user message
* optional assistant scaffolding
* optional embedded **resource references** (context pointers) that the client can attach/resolve

This is what makes prompts feel like “workflows”: they can define **multi-message interactions** and standardize how a model should proceed. ([MCP Protocol][4])

---

## What this looks like on the wire (concrete JSON-RPC examples)

### Example A — list prompts

```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "prompts/list",
  "params": { "cursor": null }
}
```

Response shape is defined in the MCP schema/spec; it returns a list of prompt descriptors (plus pagination if needed). ([Model Context Protocol][2])

### Example B — get a specific prompt with arguments

```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "prompts/get",
  "params": {
    "name": "retro.facilitate",
    "arguments": {
      "team": "Platform",
      "timebox_minutes": 60,
      "context": "Sprint 23 (Feb 10–23)"
    }
  }
}
```

The response typically includes the “compiled” prompt: a structured set of messages/instructions the client can send to the model (or show to the user before sending). ([Model Context Protocol][3])

---

## How this maps to Azure AI Foundry (deployment reality)

When you deploy an MCP server for **Foundry Agent Service**, you’re essentially hosting a remote MCP endpoint that Foundry-connected clients/agents can call. Microsoft’s guidance covers building/registering a remote MCP server and connecting it to Foundry Agent Service. ([Microsoft Learn][5])

So your “prompt reveal” story becomes:

1. Your .NET MCP server is deployed (often as a remote server, e.g., Functions / container).
2. Foundry agent/client connects to it.
3. Client calls `prompts/list`.
4. The prompt templates appear as selectable workflow starters (client UX varies).
5. On selection, client calls `prompts/get` and runs the returned message plan.

---

## Good prompt-template patterns (what to expose)

A solid MCP prompt template usually has:

* **Clear intent** (“what this workflow does”)
* **Arguments** that match user intent (small, typed inputs)
* **Guardrails** (explicit instructions, output format, stop conditions)
* **Tool hints** (if the workflow expects tool usage, say so)
* **Structured outputs** (JSON, Markdown sections, tables)—whatever your org wants standardized

This is exactly what prompts are designed for: reusable templates that clients can surface and users can explicitly choose. ([MCP Protocol][4])

---

## Example prompt templates you might publish (practical + “workflow-y”)

Below are examples that work well for enterprise MCP servers in Foundry contexts. Each is something a client could show as a “template card” with fields.

### 1) Retrospective facilitator (your current theme)

**Name:** `retro.facilitate`
**Arguments:**

* `team` (string)
* `timebox_minutes` (int)
* `context` (string)
* `format` (enum: `classic4`, `start_stop_continue`, `sailboat`)

**What `prompts/get` returns (conceptually):**

* System: “You are a neutral retro facilitator…”
* User: “Run a retro for {team}… produce sections: What went well / What didn’t / Learnings / Actions…”
* Output schema: Actions as `[owner, action, due_date, success_metric]`

This becomes a consistent “retro workflow” starter.

### 2) “Contract compliance reconciliation triage”

**Name:** `recon.triage_invoice_packet`
**Arguments:**

* `jurisdiction` (string)
* `invoice_id` (string)
* `contract_id` (string)
* `policy` (enum: strict/lenient)

**Workflow behavior:**

* Step 1: Ask the agent to load relevant resources (contract + invoice extracts).
* Step 2: Produce mismatch table (SKU, unit price, contracted price, delta, confidence).
* Step 3: Recommend next actions (request credit, escalate, ignore).

Prompts can also embed/point at resources to include context. (Resources are another core MCP primitive often used inside these workflows.) ([Model Context Protocol][6])

### 3) “Generate Azure-ready deployment artifacts”

**Name:** `foundry.deploy_checklist`
**Arguments:**

* `env` (dev/test/prod)
* `networking` (public/vnet/private)
* `auth` (api-key/managed-identity/oauth)

**Workflow:**

* Returns a step-by-step checklist + a standard output format
* Optionally instructs the model to call your MCP **tools** to fetch current subscription/resource info first, then generate artifacts.

### 4) “Support runbook: incident commander assistant”

**Name:** `ops.incident_commander`
**Arguments:**

* `service_name`
* `severity` (S1–S4)
* `symptoms`
* `start_time`

**Workflow:**

* Produces timeline template, comms templates, and decision tree
* Ends with: “Open questions for humans” + “Next 3 actions”

---

## A concrete “prompt descriptor” example (what your server returns in `prompts/list`)

This is the kind of metadata clients need to render a UI form:

```json
{
  "name": "retro.facilitate",
  "title": "Facilitate a sprint retrospective",
  "description": "Runs a structured retro and outputs decisions + action items in a standard format.",
  "arguments": [
    { "name": "team", "description": "Team name", "required": true },
    { "name": "timebox_minutes", "description": "Session length", "required": false },
    {
      "name": "format",
      "description": "Retro format",
      "required": false,
      "enum": ["classic4", "start_stop_continue", "sailboat"]
    },
    { "name": "context", "description": "Sprint/date context", "required": false }
  ]
}
```

That’s the essence of “revealing available prompts”: the protocol gives clients a standard way to list these and then fetch the compiled message plan.

---

## Implementation note for .NET MCP servers (what to build)

At minimum, your MCP server needs handlers for:

* `prompts/list` → return descriptors (+ pagination if you have many) ([Model Context Protocol][2])
* `prompts/get` → validate args, then return the structured messages/instructions ([Model Context Protocol][3])

If you tell me what kind of server you’re building (ASP.NET minimal API vs Azure Functions remote MCP, and whether you want prompts stored in code vs in a DB), I can sketch a clean pattern for:

* prompt registration (builder/DI)
* JSON schema for args
* versioning + deprecation strategy (so clients don’t break)

And if you want, I’ll tailor the example prompt set specifically to **Azure AI Foundry deployment workflows** (agent/tool catalog, environment promotion, networking/auth variants) using Microsoft’s Foundry MCP guidance as the baseline. ([Microsoft Learn][5])

[1]: https://modelcontextprotocol.io/specification/2025-11-25?utm_source=chatgpt.com "Specification"
[2]: https://modelcontextprotocol.io/specification/draft/schema?utm_source=chatgpt.com "Schema Reference"
[3]: https://modelcontextprotocol.io/specification/2025-06-18/server/prompts?utm_source=chatgpt.com "Prompts"
[4]: https://modelcontextprotocol.info/docs/concepts/prompts/?utm_source=chatgpt.com "Prompts"
[5]: https://learn.microsoft.com/en-us/azure/ai-foundry/mcp/build-your-own-mcp-server?view=foundry&utm_source=chatgpt.com "Build and register a Model Context Protocol (MCP) server"
[6]: https://modelcontextprotocol.io/specification/2025-06-18/server/resources?utm_source=chatgpt.com "Resources"
