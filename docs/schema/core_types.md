Here are the **authoritative schema/type references** you’ll want for MCP (and the C# SDK you’re using: `ModelContextProtocol` / `ModelContextProtocol.AspNetCore` **1.0.0-rc.1**).

## 1) MCP protocol schema + detailed type definitions (authoritative)

These are the “what are all the message shapes / DTOs / primitives” docs:

* **Schema Reference (all types, fields, unions, enums, etc.) — spec 2025-06-18**
  This is the most useful “type encyclopedia” page. ([Model Context Protocol][1])

* **Specification root (explains the spec is defined by `schema.ts`) — spec 2025-06-18** ([Model Context Protocol][2])

* **TypeScript source of truth (`schema.ts`) — spec 2025-06-18** ([GitHub][3])

* **Generated JSON Schema (`schema.json`) — spec 2025-06-18** (handy for validators / tooling) ([GitHub][4])
  *Also:* a browsable hosted bundle view exists here (nice for exploring references). ([schemas.sourcemeta.com][5])

## 2) Server “primitives” pages (schemas for tools/prompts/resources)

These are the best references when you’re implementing `tools/list`, `tools/call`, etc., and need the exact shapes:

* **Tools (server/tools) — spec 2025-06-18** ([Model Context Protocol][6])
* **Prompts (server/prompts) — spec 2025-06-18** ([Model Context Protocol][7])
* **Resources (server/resources) — spec 2025-06-18** ([Model Context Protocol][8])
* **Server overview (how primitives relate) — spec 2025-06-18** ([Model Context Protocol][9])

## 3) Base protocol message shapes (JSON-RPC envelopes, transports)

When you need the envelope types (request/response/notification) + how they move over HTTP/stdio:

* **Basic overview (includes “Schema is source of truth + generated JSON Schema exists”) — spec 2025-06-18** ([Model Context Protocol][10])
* **Transports (stdio + Streamable HTTP) — spec 2025-06-18** ([Model Context Protocol][11])

## 4) C# SDK type docs (your `ModelContextProtocol*` packages)

For **actual .NET types** like `RequestContext<T>`, filters, handlers, etc.:

* **C# SDK docs (conceptual + API)** ([modelcontextprotocol.github.io][12])
* **C# SDK API reference (namespaces/classes list)** ([modelcontextprotocol.github.io][13])
* **Repo (if you want to “Go to Definition” in GitHub)** ([GitHub][14])

If you want to jump straight into tool-related .NET constructs, this is also useful:

* **`McpServerToolAttribute` (how tools are exposed from C#)** ([modelcontextprotocol.github.io][15])

## 5) Microsoft Foundry’s MCP integration docs (client-side expectations)

Since you’re targeting Foundry/Agent Service compatibility, these are the relevant Microsoft docs:

* **Foundry agents: connect to MCP server endpoints (Preview)** ([Microsoft Learn][16])
* **Foundry Classic: “Connect to MCP servers”** ([Microsoft Learn][17])
* **VS Code + Foundry Classic agents using MCP tools** ([Microsoft Learn][18])

If you paste the actual `McpRequestFilter<TParams,TResult>` signature (from hover/Go To Definition), I can point you to the **exact** API doc page for that delegate + `RequestContext<T>` members in the C# SDK docs.

[1]: https://modelcontextprotocol.io/specification/2025-06-18/schema "Schema Reference - Model Context Protocol"
[2]: https://modelcontextprotocol.io/specification/2025-06-18?utm_source=chatgpt.com "Specification"
[3]: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/schema/2025-06-18/schema.ts?utm_source=chatgpt.com "modelcontextprotocol/schema/2025-06-18/schema.ts at main"
[4]: https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1638?utm_source=chatgpt.com "[Claude Desktop] Client rejects spec-compliant mixed ..."
[5]: https://schemas.sourcemeta.com/modelcontextprotocol/2025-06-18/schema?utm_source=chatgpt.com "/modelcontextprotocol/2025-06-18/schema"
[6]: https://modelcontextprotocol.io/specification/2025-06-18/server/tools "Tools - Model Context Protocol"
[7]: https://modelcontextprotocol.io/specification/2025-06-18/server/prompts?utm_source=chatgpt.com "Prompts"
[8]: https://modelcontextprotocol.io/specification/2025-06-18/server/resources?utm_source=chatgpt.com "Resources"
[9]: https://modelcontextprotocol.io/specification/2025-06-18/server?utm_source=chatgpt.com "Overview"
[10]: https://modelcontextprotocol.io/specification/2025-06-18/basic?utm_source=chatgpt.com "Overview"
[11]: https://modelcontextprotocol.io/specification/2025-06-18/basic/transports?utm_source=chatgpt.com "Transports"
[12]: https://modelcontextprotocol.github.io/csharp-sdk/?utm_source=chatgpt.com "Overview | MCP C# SDK - GitHub Pages"
[13]: https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html?utm_source=chatgpt.com "Namespace ModelContextProtocol | MCP C# SDK"
[14]: https://github.com/modelcontextprotocol/csharp-sdk?utm_source=chatgpt.com "modelcontextprotocol/csharp-sdk: The official C# SDK for ..."
[15]: https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Server.McpServerToolAttribute.html?utm_source=chatgpt.com "Class McpServerToolAttribute | MCP C# SDK - GitHub Pages"
[16]: https://learn.microsoft.com/en-au/azure/ai-foundry/agents/how-to/tools/model-context-protocol?view=foundry&utm_source=chatgpt.com "Connect to MCP Server Endpoints for agents (Preview)"
[17]: https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools-classic/model-context-protocol?view=foundry-classic&utm_source=chatgpt.com "Connect to Model Context Protocol servers (preview)"
[18]: https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/develop/vs-code-agents-mcp?view=foundry-classic&utm_source=chatgpt.com "Work with Foundry Classic agents and MCP server tools in ..."
