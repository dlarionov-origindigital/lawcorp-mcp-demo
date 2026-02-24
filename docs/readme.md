# docs/

Research notes and reference material collected during this project. Nothing here is runnable — it is background reading, spike outputs, and decision inputs.

## What Belongs Here

- MCP protocol notes and spec summaries
- Architecture research (comparisons, trade-off analyses)
- Auth / identity research
- Deep-research outputs (LLM-assisted explorations)
- Links to external references

## What Does NOT Belong Here

- Work items or task tracking → use `proj-mgmt/`
- Architectural decisions → use `proj-mgmt/decisions/`
- Source code → use `src/`

## Guides

| File | Contents |
|---|---|
| [`local-dev.md`](./local-dev.md) | **Local development guide** — MCP Inspector, Claude Desktop, VS Code/Cursor, Claude Code setup and troubleshooting |

## Subfolders

| Folder | Contents |
|---|---|
| `arch/` | Architecture comparisons and MCP spec notes (Azure Function vs Web API, prompt storage, MCP protocol overview) |
| `auth/` | Authentication and token flow research (Entra ID, OBO flow) |
| `deep-research/` | Extended LLM-assisted research outputs |
| `prompts/` | Prompt engineering notes and summaries |
| `related/` | Related protocols and technologies (Language Server Protocol) |

## Adding Notes

Drop markdown files in the relevant subfolder. No required format — these are working notes. If a research finding leads to an architectural decision, capture the outcome in `proj-mgmt/decisions/` and link back to the relevant doc here.

---

## MCP Reference Links

- [MCP Specification](https://modelcontextprotocol.io/specification/2025-03-26)
- [Build an MCP server in C# — MS Learn](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
- [Microsoft C# MCP SDK announcement](https://devblogs.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol)
