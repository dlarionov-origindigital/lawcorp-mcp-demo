# Epic 1: Project Foundation & Infrastructure

**Status:** IN PROGRESS
**Goal:** Stand up the .NET solution structure, MCP server skeleton, authentication with Microsoft Entra ID, and the custom authorization layer.

## Features

| ID | Feature | Status |
|---|---|---|
| [1.1](./1.1-solution-structure.md) | Solution & Project Structure | IN PROGRESS |
| [1.2](./1.2-authentication.md) | Authentication — Microsoft Entra ID | BACKLOG |
| [1.3](./1.3-authorization.md) | Custom Authorization Layer | BACKLOG |

## Dependencies

Depends on: None
Blocks: Epic 2, Epic 3, Epic 4, Epic 5, Epic 6

## Success Criteria

- [x] Solution builds cleanly with all projects referenced
- [x] MCP server skeleton responds to protocol messages
- [ ] Entra ID tokens are validated end-to-end
- [ ] Role-based authorization enforced on all tool calls
- [ ] Row-level security filters data by user role
- [ ] Audit log captures all data access
