# Epic 5: MCP Prompts & Sampling

**Status:** BACKLOG
**Goal:** Implement all 12 reusable prompt templates and 4 server-initiated sampling use cases that encode Law-Corp domain workflows.

## Features

| ID | Feature | Status |
|---|---|---|
| [5.1](./5.1-prompt-templates.md) | Prompt Templates | BACKLOG |
| [5.2](./5.2-sampling.md) | Sampling (Server-Initiated LLM Calls) | BACKLOG |

## Dependencies

Depends on: [Epic 2](../02-data-model/_epic.md), [Epic 3](../03-mcp-tools/_epic.md)
Blocks: [Epic 6](../06-protocol-deployment/_epic.md)

## Success Criteria

- [ ] All 12 prompts registered and discoverable via `prompts/list`
- [ ] Each prompt produces well-structured, domain-appropriate output
- [ ] Sampling use cases demonstrably enhance tool capabilities
