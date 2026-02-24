# Research: stdio vs HTTP Transport for MCP Servers

**Date:** 2026-02-24
**Related ADR:** [ADR-004](../../decisions/004-dual-transport-web-api-primary.md)
**Related Task:** [1.1.2](./1.1.2-configure-mcp-skeleton.md)

---

## Summary

The MCP specification (version 2025-03-26) defines **two standard transports**: stdio and Streamable HTTP. SSE (the original HTTP transport) is deprecated. This research evaluates how the transport choice impacts testing, local chat GUI integration, and whether a dual-transport `appsettings`-based switch is a reasonable architecture for this project.

---

## 1. Transport Characteristics

| Dimension | stdio | Streamable HTTP |
|---|---|---|
| **Protocol** | JSON-RPC 2.0 over stdin/stdout pipes | JSON-RPC 2.0 over a single HTTP POST endpoint with optional SSE streaming |
| **Client model** | Single client; host spawns server as child process | Multi-client; server listens on a port |
| **Latency** | Sub-millisecond (no network stack) | 10–50 ms (HTTP parsing + network) |
| **Throughput** | ~10,000 ops/sec | ~100–1,000 ops/sec (depends on network) |
| **Memory per connection** | ~10 MB | ~50 MB |
| **Remote access** | Not supported | Supported |
| **Session management** | Implicit (process lifetime) | Explicit (`Mcp-Session-Id` header) |
| **Debugging** | Attach to single process | Standard HTTP tooling (curl, Postman, browser dev tools) |

> **Key takeaway:** stdio is inherently local and single-client. Streamable HTTP is the only viable transport for remote, multi-client, and production cloud scenarios.

### Sources

- [MCP Specification — Transports (2025-03-26)](https://modelcontextprotocol.io/specification/2025-03-26/basic/transports)
- [MCPcat — Comparing stdio vs SSE vs StreamableHTTP](https://mcpcat.io/guides/comparing-stdio-sse-streamablehttp)
- [MCP Blog — Future of MCP Transports (2025-12-19)](https://blog.modelcontextprotocol.io/posts/2025-12-19-mcp-transport-future/)

---

## 2. Impact on Testing

### stdio Testing

- Tests must **spawn the server as a child process** and communicate over pipes.
- Every test becomes a process-boundary integration test — slower, harder to isolate, and fragile on CI (process lifetime, pipe encoding, JSON-RPC framing).
- No `WebApplicationFactory<Program>` support; the standard ASP.NET Core test infrastructure doesn't apply.
- Debugging requires attaching to the child process.
- The C# SDK's `StdioServerTransport` is designed for production hosting, not test harnesses.

### HTTP Testing

- `WebApplicationFactory<Program>` creates an **in-memory test server** — no process boundary, no port binding, sub-second startup.
- Tests use a standard `HttpClient` to send JSON-RPC requests over HTTP.
- DI container is fully accessible: swap `IFirmIdentityContext` with `FakeIdentityContext`, swap SQL Server with SQLite in-memory, etc.
- Health checks, auth middleware, and MCP tool endpoints are all testable through the same HTTP surface.
- Integration tests run in **< 2 seconds** on CI.

### Recommendation for Testing

**HTTP mode is the primary test surface.** stdio should only have a minimal smoke test (spawn process, send `initialize`, verify handshake) marked as `[Trait("Category", "E2E")]` and excluded from the default CI run.

### Sources

- [MCPcat — MCP Integration Testing Guide](https://mcpcat.io/guides/integration-tests-mcp-flows)
- [Grizzly Peak Software — Testing MCP Servers: Unit and Integration](https://www.grizzlypeaksoftware.com/library/testing-mcp-servers-unit-and-integration-8d4qt3ob)
- [MCP C# SDK — StdioServerTransport API](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Server.StdioServerTransport.html)
- [MCP C# SDK — HttpMcpServerBuilderExtensions](https://modelcontextprotocol.github.io/csharp-sdk/api/Microsoft.Extensions.DependencyInjection.HttpMcpServerBuilderExtensions.html)

---

## 3. Impact on Local Chat GUI Integration

### Claude Desktop

- **Transport:** stdio only (as of Feb 2026). Claude Desktop launches the MCP server as a subprocess.
- **Config file:** `claude_desktop_config.json` (Windows: `%APPDATA%\Claude\claude_desktop_config.json`)
- **Config format:**
  ```json
  {
    "mcpServers": {
      "lawcorp": {
        "command": "dotnet",
        "args": ["run", "--project", "path/to/LawCorp.Mcp.Server"],
        "env": {
          "Transport__Mode": "stdio"
        }
      }
    }
  }
  ```
- Claude Desktop **cannot connect to HTTP-based MCP servers**. stdio is mandatory for this client.

### Claude Code (CLI)

- **Transport:** stdio and HTTP. Configured via `claude mcp add`.
- **stdio:** `claude mcp add --transport stdio lawcorp -- dotnet run --project path/to/LawCorp.Mcp.Server`
- **HTTP:** `claude mcp add --transport http lawcorp http://localhost:5000/mcp`

### VS Code (Copilot Chat / MCP Extensions)

- **Transport:** stdio and HTTP (`"type": "stdio"` or `"type": "http"` in `.vscode/mcp.json`).
- **stdio config:**
  ```json
  {
    "servers": {
      "lawcorp": {
        "type": "stdio",
        "command": "dotnet",
        "args": ["run", "--project", "path/to/LawCorp.Mcp.Server"],
        "env": {
          "Transport__Mode": "stdio"
        }
      }
    }
  }
  ```
- **HTTP config:**
  ```json
  {
    "servers": {
      "lawcorp": {
        "type": "http",
        "url": "http://localhost:5000/mcp"
      }
    }
  }
  ```
- VS Code tries Streamable HTTP first, falls back to SSE if `type` is `"http"`.

### Cursor

- Uses the same `.vscode/mcp.json` or `.cursor/mcp.json` format as VS Code.
- Supports both stdio and HTTP transport types.

### MCP Inspector (Development Tool)

- Expects SSE by default; can be configured for stdio via `--stdio` flag or proxy.
- Useful for interactive tool testing during development.

### Sources

- [Claude Desktop MCP Setup Guide (2026)](https://mcpplaygroundonline.com/blog/how-to-setup-mcp-claude-desktop)
- [VS Code — MCP Configuration Reference](https://code.visualstudio.com/docs/copilot/reference/mcp-configuration)
- [VS Code — MCP Developer Guide](https://code.visualstudio.com/api/extension-guides/mcp)
- [Claude Code — Connect to tools via MCP](https://code.claude.com/docs/en/mcp)

---

## 4. Key Decision: Dual Transport via `appsettings`

### Current Design (ADR-004)

ADR-004 proposes a `Transport:Mode` config value (`"http"` or `"stdio"`) that selects the transport at startup. The ASP.NET Core pipeline is always present; in `stdio` mode, Kestrel simply doesn't start.

### Is This Reasonable?

**Yes — with caveats.** The pattern is sound and aligns with how the C# MCP SDK is designed:

| Pro | Con |
|---|---|
| Tool/resource/prompt code is transport-agnostic (SDK design) | Two code paths in `Program.cs` that must both be maintained and tested |
| One deployable artifact for all scenarios | `stdio` mode is a secondary path that may accumulate drift if not smoke-tested regularly |
| Simple for developers: set one config value | Claude Desktop requires the env var override `Transport__Mode=stdio`; forgetting it causes a confusing Kestrel startup |
| `WebApplicationFactory` tests validate the HTTP path naturally | stdio smoke test requires process spawning (heavier, excluded from default CI) |

### Industry Precedent

The halter73/console-to-http-mcp-server reference repo on GitHub demonstrates the same pattern: a single .NET app that can run in either stdio or HTTP mode based on configuration. Microsoft's own Azure App Service MCP tutorial uses `ModelContextProtocol.AspNetCore` with `WithHttpTransport()` for the web path.

### Risk: Config Confusion in Demo Scenarios

The biggest practical risk is **demo confusion**: when demonstrating the server to stakeholders, switching between Claude Desktop (stdio), VS Code (either), and a browser-based test client (HTTP) requires changing or overriding the transport config. If this is not scripted or documented clearly, the demo breaks.

**Mitigation:** Default to `http` mode (the more capable mode). Claude Desktop and other stdio clients pass `Transport__Mode=stdio` as an environment variable in their launch config — no manual switching needed.

### Sources

- [halter73/console-to-http-mcp-server (GitHub)](https://github.com/halter73/console-to-http-mcp-server)
- [Microsoft Learn — Web app as MCP server in GitHub Copilot Chat (.NET)](https://learn.microsoft.com/en-us/azure/app-service/tutorial-ai-model-context-protocol-server-dotnet)
- [MCP C# SDK — GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP Best Practices](https://mcp-best-practice.github.io/mcp-best-practice/best-practice/)

---

## 5. SDK Package Requirements

The current project uses `ModelContextProtocol` v0.9.0-preview.2 with `WithStdioServerTransport()`. To support dual transport:

| Package | Purpose | Required For |
|---|---|---|
| `ModelContextProtocol` | Core SDK, DI extensions, tool discovery | Both transports |
| `ModelContextProtocol.AspNetCore` | `WithHttpTransport()` extension method, Streamable HTTP support | HTTP mode |
| `Microsoft.AspNetCore.App` (framework ref) | ASP.NET Core pipeline, Kestrel, `WebApplication` | HTTP mode + tests |

The migration from `Host.CreateApplicationBuilder()` to `WebApplication.CreateBuilder()` is the prerequisite for adding the ASP.NET Core package.

### Sources

- [NuGet — ModelContextProtocol packages](https://www.nuget.org/profiles/ModelContextProtocol)
- [MCP C# SDK — Overview](https://modelcontextprotocol.github.io/csharp-sdk/)
- [Microsoft Learn — Build an MCP server (.NET quickstart)](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-server)

---

## 6. Conclusion

| Question | Answer |
|---|---|
| Should we support both stdio and HTTP? | **Yes.** Claude Desktop requires stdio; testing and production require HTTP. |
| Is `appsettings` config switching reasonable? | **Yes.** It's the established pattern in the .NET MCP ecosystem. Default to HTTP; stdio clients override via env var. |
| What's the primary test transport? | **HTTP** via `WebApplicationFactory`. stdio gets a minimal smoke test only. |
| What NuGet package do we need to add? | `ModelContextProtocol.AspNetCore` for `WithHttpTransport()`. |
| What's the biggest risk? | Demo confusion from transport mismatch. Mitigate with clear docs and default-to-HTTP. |
