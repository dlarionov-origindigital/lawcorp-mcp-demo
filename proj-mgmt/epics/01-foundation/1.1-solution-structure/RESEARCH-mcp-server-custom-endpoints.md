# Research: MCP Server Custom Endpoints & Azure App Service Health Checks

**Status:** COMPLETE
**Date:** 2026-02-25
**Context:** Determine whether the MCP server should expose custom HTTP endpoints (health checks, OpenAPI, diagnostics) alongside the MCP protocol endpoint, and how this maps to Azure App Service deployment.

---

## Research Questions

1. Should an MCP server host custom REST endpoints alongside the MCP protocol endpoint?
2. What health check patterns does Azure App Service require?
3. What is Microsoft's recommended approach for MCP servers on App Service?
4. How does this affect the existing architecture?

---

## 1. MCP + Custom Endpoints: Standard Practice

**Finding: Yes — it is standard and recommended to host custom endpoints alongside MCP.**

Microsoft's official App Service MCP tutorial explicitly demonstrates this pattern. The [App Service MCP Server tutorial (learn.microsoft.com)](https://learn.microsoft.com/en-us/azure/app-service/tutorial-ai-model-context-protocol-server-dotnet) shows an ASP.NET Core app that:
- Has existing REST controllers (e.g., `TodosController`)
- Adds MCP via `builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly()`
- Maps MCP to a custom path: `app.MapMcp("/api/mcp")`
- Keeps the existing controllers, middleware, CORS, and static files

The MCP endpoint is just another route in an ASP.NET Core app. Nothing prevents coexisting endpoints.

**Key quote from the tutorial:**
> "A best practice would be to move the app logic to a service class, then call the service methods both from `TodosController` and from `TodosMcpTool`."

This validates our architecture — MediatR handlers are our "service class," called by both MCP tools and future REST controllers.

### References
- [Integrate an App Service app as an MCP Server for GitHub Copilot Chat (.NET)](https://learn.microsoft.com/en-us/azure/app-service/tutorial-ai-model-context-protocol-server-dotnet)
- [Connect and govern existing MCP server in Azure API Management](https://learn.microsoft.com/en-us/azure/api-management/expose-existing-mcp-server)
- [Self-hosted remote MCP server on Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions/self-hosted-mcp-servers)

---

## 2. Azure App Service Health Checks

**Finding: Health check endpoints are required for production Azure App Service deployments.**

Azure App Service pings a configured health check path every minute. If an instance fails **10 consecutive checks**, it is marked unhealthy and removed from load balancer rotation. After **one hour** of remaining unhealthy, the instance is **replaced**.

### Configuration Methods
- **Azure Portal:** Monitoring → Health check → set path (e.g., `/health`)
- **Azure CLI:** `az webapp config set --generic-configurations '{"healthCheckPath": "/health"}'`
- **ARM/Bicep:** `siteConfig.healthCheckPath` property

### Health Check Tiers

| Type | Endpoint | Checks | Use Case |
|---|---|---|---|
| Liveness | `/health` | Process is running | App Service instance monitoring |
| Readiness | `/health/ready` | DB connected, external APIs reachable | Traffic routing decisions |
| Startup | `/health/startup` | Migrations complete, caches warm | Prevent premature traffic |

### ASP.NET Core Built-in Support

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(5))
    .AddUrlGroup(new Uri("http://localhost:5002/health"), "external-api");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### References
- [Health monitoring in ASP.NET Core (learn.microsoft.com)](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Azure App Service Health Check (learn.microsoft.com)](https://learn.microsoft.com/en-us/azure/app-service/monitor-instances-health-check)

---

## 3. Recommended Custom Endpoints for the MCP Server

Based on research, the MCP Server should expose these endpoints alongside `MapMcp()`:

| Endpoint | Purpose | Auth Required |
|---|---|---|
| `/health` | Liveness probe (App Service, k8s) | No |
| `/health/ready` | Readiness probe (DB + External API reachable) | No |
| `/openapi/v1.json` | OpenAPI spec for custom endpoints (dev only) | No |
| `/api/mcp` | MCP protocol (Streamable HTTP + SSE) | Yes (JWT Bearer) |

### Why `/api/mcp` instead of root

The current code maps MCP to root: `app.MapMcp()`. For production App Service deployment, we should use `app.MapMcp("/api/mcp")` so:
- Health checks don't conflict with MCP routing
- API Management can proxy the MCP endpoint on a known prefix
- Standard REST endpoints (health, OpenAPI, diagnostics) live outside the MCP path

---

## 4. Impact on Current Architecture

### What already exists
- External API has `HealthController` at `/health`
- MCP Server has no health endpoint
- MCP endpoint is at root (`/`)
- OpenAPI added for Development environment

### What needs to change

| Change | Impact | Story |
|---|---|---|
| Add ASP.NET Core health checks to MCP Server | New endpoint, NuGet package | New story under 1.1 |
| Move MCP endpoint from `/` to `/api/mcp` | Breaking for existing MCP Inspector config | Coordinate with docs |
| Add health checks to External API (replace manual `HealthController`) | Replace existing controller with framework health checks | Update 1.4 |
| Add readiness checks (SQL + External API URL) | Validates runtime dependencies | Same story |

---

## 5. Recommendations

1. **Phase 1 (current milestone):** Add basic health endpoints to both services. Keep MCP at root for now — changing to `/api/mcp` is a separate story when we target App Service deployment.
2. **Phase 2 (Epic 6: Deployment):** Move MCP to `/api/mcp`, add readiness probes with dependency checks, add API Management integration, configure App Service health check settings in Bicep/ARM.
3. **Track as a new epic** — "Deployment & Operations" (Epic 7 or fold into existing Epic 6) with stories for health checks, App Service config, API Management MCP proxy, and monitoring.

---

## 6. Authoritative References

1. [Integrate an App Service app as an MCP Server — Microsoft Learn](https://learn.microsoft.com/en-us/azure/app-service/tutorial-ai-model-context-protocol-server-dotnet)
2. [Connect and govern existing MCP server in Azure API Management — Microsoft Learn](https://learn.microsoft.com/en-us/azure/api-management/expose-existing-mcp-server)
3. [Self-hosted remote MCP server on Azure Functions — Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-functions/self-hosted-mcp-servers)
4. [Health checks in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
5. [Monitor App Service instances using health check — Microsoft Learn](https://learn.microsoft.com/en-us/azure/app-service/monitor-instances-health-check)
6. [Quickstart: Create a minimal MCP server (.NET) — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-server)
7. [Secure MCP calls to Azure App Service with Entra Auth — Microsoft Learn](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-mcp-server-vscode)
8. [MCP Specification: Transports — modelcontextprotocol.io](https://modelcontextprotocol.io/specification/2025-03-26/basic/transports)
9. [Connect to MCP servers (Azure AI Foundry) — Microsoft Learn](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/model-context-protocol)
10. [MCP Server on Copilot Studio — Microsoft Learn](https://learn.microsoft.com/en-us/microsoft-copilot-studio/mcp-create-new-server)
