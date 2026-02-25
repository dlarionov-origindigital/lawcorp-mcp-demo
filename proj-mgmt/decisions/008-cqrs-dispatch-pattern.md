# ADR-008: CQRS dispatch pattern for MCP tool handlers

**Status:** Proposed
**Date:** 2026-02-25

## Context

MCP tools in the Law-Corp server currently inject EF Core `DbContext` directly and execute queries inline. This worked well for the initial prototype where all data was local, but the addition of an external downstream API ([Feature 1.4](../epics/01-foundation/1.4-external-api/1.4-external-api.md)) means some tools need to:

1. Call an external REST API via OBO token exchange (network call)
2. Query the local database via EF Core (in-process call)
3. Call Microsoft Graph via OBO (network call)

Without an abstraction layer, each tool would need to know which data source to use, how to acquire OBO tokens, and how to handle errors from each source. This is the wrong layer for those concerns — tools should describe **what** they need, not **how** to get it.

Additionally, cross-cutting concerns (authorization checks, audit logging, input validation) are currently handled at the MCP protocol boundary (`ToolPermissionFilters`) or scattered through tool code. A pipeline pattern would centralize these.

### Alternatives considered

| Option | Pros | Cons |
|---|---|---|
| **Repository pattern** | Familiar, simple interfaces | Doesn't handle cross-cutting concerns; repositories tend to accumulate methods and become god-objects |
| **Service layer** | Groups related operations | Still couples callers to specific service classes; no pipeline |
| **MediatR CQRS** | Decoupled dispatch, pipeline behaviors, testable handlers | Adds a dependency; indirection can obscure call paths |
| **Hand-rolled dispatcher** | No external dependency; full control | Must build pipeline, handler resolution, and DI integration manually |

## Decision

Adopt **MediatR** as the CQRS dispatch mechanism for MCP tool handlers.

### Key design principles

1. **Tools are thin dispatchers.** A tool constructs a `IRequest<T>` (command or query) and calls `IMediator.Send()`. It formats the result as an MCP response. It does not inject `DbContext`, `HttpClient`, or `IDownstreamTokenProvider`.

2. **Handlers resolve data sources.** Each `IRequestHandler<TRequest, TResult>` knows how to fulfill the request — local DB query, external API call, Graph API call, or a combination. Handlers inject the appropriate services.

3. **Pipeline behaviors are cross-cutting.** `IPipelineBehavior<TRequest, TResponse>` implementations handle authorization, audit logging, validation, and caching without handler knowledge.

4. **Contracts live in Core.** `IRequest<T>` and result types are defined in `LawCorp.Mcp.Core` so both the server (dispatching) and handlers (implementing) can reference them without circular dependencies.

5. **Handlers live in a dedicated project.** `LawCorp.Mcp.Server.Handlers` references Core and Data. The server project references Handlers and registers its assembly with MediatR.

### Architecture

```
MCP Tool (Server)
  │
  ├── Constructs SearchCasesQuery (Core)
  ├── Calls IMediator.Send(query)
  │
  ▼
MediatR Pipeline
  ├── ValidationBehavior
  ├── AuthorizationBehavior
  ├── AuditLoggingBehavior
  │
  ▼
Handler (Server.Handlers)
  ├── SearchCasesHandler → injects DbContext, IFirmIdentityContext
  ├── SearchDocumentsHandler → injects IDownstreamTokenProvider, HttpClient (External API)
  └── GetCalendarHandler → injects IDownstreamTokenProvider (Graph)
```

### Package

MediatR 12.x via NuGet. If the licensing model becomes a concern, the interface surface (`IMediator`, `IRequest<T>`, `IRequestHandler<T,R>`, `IPipelineBehavior<T,R>`) is small enough to implement manually as a fallback.

## Consequences

### What becomes easier

- **Adding new data sources** — new handler, same tool interface. Tools don't change when a data source moves from local DB to external API.
- **Cross-cutting concerns** — authorization, audit, validation, caching applied uniformly via pipeline behaviors.
- **Testing** — handlers are unit-testable in isolation; tools can be tested by mocking `IMediator`.
- **External API integration** — the external API handler ([1.5.5](../epics/01-foundation/1.5-cqrs-dispatch/1.5.5-external-api-handler.md)) acquires OBO tokens and calls the API without the tool knowing.

### What becomes harder

- **Indirection** — navigating from a tool to its handler requires following the dispatch chain. IDE "Go to Implementation" on `IMediator.Send()` doesn't jump to the handler.
- **Boilerplate** — each operation requires a request type, result type, and handler class (3 files minimum). For simple queries, this feels heavy.
- **MediatR dependency** — adds a third-party package. MediatR 12.x changed its licensing model. The team should evaluate this.
- **Learning curve** — developers unfamiliar with MediatR/CQRS need onboarding.

## References

- [MediatR documentation](https://mediatr.io/)
- [CQRS with MediatR in .NET (2026)](https://oneuptime.com/blog/post/2026-01-28-cqrs-mediatr-dotnet/view)
- [Simple CQRS Dispatcher (MediatR alternative)](https://michaeldugmore.com/p/mediatr/)
- [RESEARCH: App registration architecture](../epics/01-foundation/1.2-authn/1.2.3-app-registrations-docs/RESEARCH-app-registration-architecture.md)
- [Feature 1.4: External Downstream API](../epics/01-foundation/1.4-external-api/1.4-external-api.md)
- [Feature 1.5: CQRS Dispatch Pattern](../epics/01-foundation/1.5-cqrs-dispatch/1.5-cqrs-dispatch.md)
