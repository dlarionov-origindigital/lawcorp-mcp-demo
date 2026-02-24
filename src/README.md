# src/

The .NET 9 solution for the Law-Corp MCP platform. All runnable code, domain models, data access, mock data generation, tests, and the web application live here.

## Projects

| Project | Type | Purpose |
|---|---|---|
| `LawCorp.Mcp.Server` | Executable | MCP stdio host — registers tools, resources, and prompts; entry point |
| `LawCorp.Mcp.Core` | Class library | Domain models and enums shared across all projects |
| `LawCorp.Mcp.Data` | Class library | EF Core `DbContext`, entity configurations, migrations |
| `LawCorp.Mcp.MockData` | Class library | Deterministic seeder that populates the database with realistic test data |
| [`LawCorp.Mcp.Web`](./LawCorp.Mcp.Web/README.md) | Web app | Blazor Web App — MCP client demo and E2E test harness (Entra ID auth, Fluent UI) |
| `LawCorp.Mcp.Tests` | xUnit | Unit and integration tests |
| [`LawCorp.Mcp.Tests.E2E`](./LawCorp.Mcp.Tests.E2E/README.md) | xUnit + Playwright | E2E browser tests — Entra ID login automation, persona-based access validation |

## Project References

```
Server   →  Core
Web      →  Core
Data     →  Core
MockData →  Core, Data
Tests    →  Core, Data, Server
Tests.E2E → Web, MockData
```

## Development Setup

**Prerequisites**

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server Express (local instance at `.\SQLEXPRESS`) — or update the connection string in `appsettings.Development.json`

**Build**

```bash
dotnet build LawCorp.Mcp.sln
```

**Run the server**

```bash
dotnet run --no-launch-profile --project LawCorp.Mcp.Server
```

The server uses stdio transport — it is not an HTTP server. Connect via Claude Desktop or the MCP inspector tool. The `--no-launch-profile` flag is required to prevent `dotnet run` from printing launch profile info to stdout, which corrupts the JSON-RPC stream.

**Run the web app**

```bash
dotnet run --project LawCorp.Mcp.Web --launch-profile https
```

Opens at `https://localhost:5001`. See [`LawCorp.Mcp.Web/README.md`](./LawCorp.Mcp.Web/README.md) for auth configuration and full details.

**Run tests**

```bash
dotnet test LawCorp.Mcp.sln
```

**Seed mock data**

Mock data seeding is built into `LawCorp.Mcp.MockData`. Call `MockDataSeeder.SeedAsync(db)` with an EF Core `DbContext` instance. Seeding is idempotent (checks `db.Attorneys.AnyAsync()` before inserting). The seed is deterministic (`Random(42)`).

## Configuration

Local development configuration lives in `LawCorp.Mcp.Server/appsettings.Development.json` (git-ignored). Copy from `appsettings.Development.json.example` and update the connection string for your environment.

## Notes

- The old `law-corp-mcp/` folder is a leftover from initial scaffolding and can be deleted once Visual Studio releases its lock on it.
