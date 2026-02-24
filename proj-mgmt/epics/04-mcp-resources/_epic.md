# Epic 4: MCP Resources

**Status:** BACKLOG
**Goal:** Implement all MCP resources — read-only data endpoints the LLM can access for contextual information, including static reference data, dynamic URI-templated resources, and subscription-based notifications.

## Features

| ID | Feature | Status |
|---|---|---|
| [4.1](./4.1-static-resources.md) | Static Resources | BACKLOG |
| [4.2](./4.2-dynamic-resources.md) | Dynamic Resources (URI Templates) | BACKLOG |
| [4.3](./4.3-subscription-resources.md) | Subscription Resources (Notifications) | BACKLOG |

## Dependencies

Depends on: [Epic 2](../02-data-model/_epic.md), [Epic 1](../01-foundation/_epic.md)
Blocks: [Epic 6](../06-protocol-deployment/_epic.md)

## Success Criteria

- [ ] All static resources return well-structured JSON
- [ ] Dynamic resources correctly resolve URI templates with authorization
- [ ] Subscription resources push notifications when triggers fire
