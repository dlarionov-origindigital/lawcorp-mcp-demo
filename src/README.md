# src/

The .NET 9 solution for the Law-Corp MCP platform. All runnable code, domain models, data access, mock data generation, tests, and the web application live here.

## Projects

| Project | Type | Port | Purpose |
|---|---|---|---|
| `LawCorp.Mcp.Server` | Executable | 5000 | MCP server (HTTP + stdio transport), dispatches tools via MediatR |
| `LawCorp.Mcp.Server.Handlers` | Class library | — | MediatR command/query handlers (local DB + external API) |
| `LawCorp.Mcp.ExternalApi` | Web API | 5002 | Independent DMS API (JWT Bearer, receives OBO tokens, own database) |
| `LawCorp.Mcp.Core` | Class library | — | Domain models, enums, MediatR contracts shared across all projects |
| `LawCorp.Mcp.Data` | Class library | — | EF Core `DbContext`, entity configurations, migrations |
| `LawCorp.Mcp.MockData` | Class library | — | Deterministic seeder that populates the MCP server database |
| [`LawCorp.Mcp.Web`](./LawCorp.Mcp.Web/README.md) | Web app | 5001/5003 | Blazor Web App — MCP client demo and E2E test harness |
| `LawCorp.Mcp.Tests` | xUnit | — | Unit and integration tests |
| [`LawCorp.Mcp.Tests.E2E`](./LawCorp.Mcp.Tests.E2E/README.md) | xUnit + Playwright | — | E2E browser tests |

## Project References

```
Server     →  Core, Data, MockData, Server.Handlers
Handlers   →  Core, Data
ExternalApi → Core
Web        →  Core
Data       →  Core
MockData   →  Core, Data
Tests      →  Core, Data, Server
Tests.E2E  →  Web, MockData
```

## Development Setup

**Prerequisites**

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server Express (local instance at `.\SQLEXPRESS`) — or update the connection string in `appsettings.Development.json`

**Build**

```bash
dotnet build LawCorp.Mcp.sln
```

**Run the MCP server (stdio)**

```bash
dotnet run --no-launch-profile --project LawCorp.Mcp.Server
```

The `--no-launch-profile` flag is required to prevent `dotnet run` from printing launch profile info to stdout, which corrupts the JSON-RPC stream.

**Run the MCP server (HTTP) + External API**

```bash
# Terminal 1 — External API (DMS) on port 5002
dotnet run --project LawCorp.Mcp.ExternalApi --launch-profile http

# Terminal 2 — MCP Server on port 5000
dotnet run --project LawCorp.Mcp.Server --launch-profile http
```

**Run the web app**

```bash
dotnet run --project LawCorp.Mcp.Web --launch-profile https
```

Opens at `https://localhost:5001`.

See [`docs/local-dev.md`](../docs/local-dev.md) for the complete multi-service setup guide.

**Run tests**

```bash
dotnet test LawCorp.Mcp.sln
```

**Seed mock data**

Mock data seeding is built into `LawCorp.Mcp.MockData`. Call `MockDataSeeder.SeedAsync(db)` with an EF Core `DbContext` instance. Seeding is idempotent (checks `db.Users.AnyAsync()` before inserting). The seed is deterministic (`Random(42)`).

## Configuration

Local development configuration lives in `LawCorp.Mcp.Server/appsettings.Development.json` (git-ignored). Copy from `appsettings.Development.json.example` and update the connection string for your environment.

## Notes

- The old `law-corp-mcp/` folder is a leftover from initial scaffolding and can be deleted once Visual Studio releases its lock on it.
