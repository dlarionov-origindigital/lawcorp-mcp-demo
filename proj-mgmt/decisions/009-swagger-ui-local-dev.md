# ADR-009: Add interactive Swagger UI for local development ergonomics

**Status:** Accepted
**Date:** 2026-02-25

## Context

Both the MCP Server (HTTP transport) and External API expose OpenAPI specs at `/openapi/v1.json` in the Development environment ([1.1.4](../epics/01-foundation/1.1-solution-structure/1.1.4-openapi-swagger-config.md)). However, to explore and test endpoints interactively, developers must copy the JSON URL into an external tool (Swagger Editor, Postman, VS Code REST Client). This adds friction to the inner dev loop — especially for new team members onboarding to the project.

.NET 9 removed the Swashbuckle auto-inclusion from `webapi` templates and replaced it with the lighter `Microsoft.AspNetCore.OpenApi` spec generator. The spec-only approach is intentional for production, but we need an interactive UI for development and pre-production environments.

### Requirements

1. An interactive API documentation UI should be available when running services locally.
2. The UI must work with the existing `.NET 9 AddOpenApi() / MapOpenApi()` pipeline — no duplicate spec generation.
3. Swagger UI should be available in Development, QA, and UAT environments but **not** in Production.
4. The browser should auto-open to the Swagger UI when launching services in Development via `launchSettings.json`.

### Alternatives considered

| Option | Pros | Cons |
|---|---|---|
| **Swashbuckle.AspNetCore** | Well-known, battle-tested | Deprecated in .NET 9 templates; generates its own spec (duplicates `AddOpenApi()`); heavy |
| **NSwag** | Powerful code generation, Swagger UI included | Heavier; opinionated about spec generation; duplicates built-in pipeline |
| **Scalar.AspNetCore** | Designed for .NET 9 `MapOpenApi()`; modern UI; single NuGet; lightweight | Newer project; less widespread than Swashbuckle |
| **External tools only** (current) | No added dependencies | Friction: developers must manually load spec URLs; inconsistent team experience |

## Decision

Add **Scalar.AspNetCore** to both the MCP Server and External API projects as the interactive API documentation UI.

### Key design principles

1. **Environment-gated.** Scalar UI is mapped only when `!Environment.IsProduction()`. This covers Development, QA, and UAT without an explicit allowlist. A `Production` environment never serves the UI.

2. **Spec generation unchanged.** The existing `AddOpenApi()` / `MapOpenApi()` pipeline continues to generate the spec. Scalar consumes it — no duplicate generation.

3. **Auto-open in Development.** `launchSettings.json` profiles set `launchBrowser: true` and `launchUrl` to the Scalar UI path so the browser opens automatically when F5-ing or `dotnet run`-ing with the `http` profile.

4. **No config key needed.** Environment detection (`IsProduction()`) is sufficient. If a future need arises for finer control, a `EnableSwaggerUI` appsettings key can be added without breaking changes.

### Endpoints

| Service | Scalar UI | OpenAPI Spec |
|---|---|---|
| MCP Server (HTTP) | `http://localhost:5000/scalar/v1` | `http://localhost:5000/openapi/v1.json` |
| External API | `http://localhost:5002/scalar/v1` | `http://localhost:5002/openapi/v1.json` |

## Consequences

### What becomes easier

- **Developer onboarding** — starting either service immediately opens an interactive API explorer; no external tooling required.
- **Endpoint discovery** — developers can see all routes, schemas, and try-it-out requests from the browser.
- **Pre-production validation** — QA/UAT environments also serve the UI, making it easier to verify deployed API shapes.

### What becomes harder

- **Dependency surface** — adds one NuGet package (`Scalar.AspNetCore`) per project. This is a runtime dependency in non-production builds (though it serves only static assets and a redirect).
- **Accidental exposure** — if `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is misconfigured in production, the UI would be served. Mitigated by the environment variable being a standard deployment concern.

## References

- [Scalar.AspNetCore — NuGet](https://www.nuget.org/packages/Scalar.AspNetCore)
- [Scalar — GitHub](https://github.com/scalar/scalar)
- [.NET 9 OpenAPI migration (Swashbuckle removal)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-9.0)
- [1.1.4: OpenAPI / Swagger Configuration](../epics/01-foundation/1.1-solution-structure/1.1.4-openapi-swagger-config.md)
