# MCP C# SDK — Client API Reference

**Package:** `ModelContextProtocol` `1.0.0-rc.1`
**Namespace:** `ModelContextProtocol.Client` + `ModelContextProtocol.Protocol`
**Source of truth:** [modelcontextprotocol.github.io/csharp-sdk/api](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.Client.html)

---

## McpClient

**Class** — `ModelContextProtocol.Client.McpClient`

The concrete MCP client. Use the static factory to create one.

```csharp
McpClient client = await McpClient.CreateAsync(
    IClientTransport transport,
    McpClientOptions? options = null,
    ILoggerFactory? loggerFactory = null,
    CancellationToken cancellationToken = default);
```

Key methods:

| Method | Returns | Notes |
|--------|---------|-------|
| `ListToolsAsync(RequestOptions?, CancellationToken)` | `ValueTask<IList<McpClientTool>>` | Calls `tools/list` |
| `CallToolAsync(string, IReadOnlyDictionary<string,object?>, IProgress?, RequestOptions?, CancellationToken)` | `ValueTask<CallToolResult>` | Calls `tools/call` |
| `ListResourcesAsync(...)` | `ValueTask<IList<McpClientResource>>` | |
| `ReadResourceAsync(Uri, ...)` | `ValueTask<ReadResourceResult>` | |
| `ListPromptsAsync(...)` | `ValueTask<IList<McpClientPrompt>>` | |

---

## McpClientTool

**Class** — `ModelContextProtocol.Client.McpClientTool`
Inherits `AIFunction` (Microsoft.Extensions.AI).

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Tool name as registered on the server |
| `Description` | `string` | Human-readable description |
| `Title` | `string?` | Optional display title |
| `JsonSchema` | `JsonElement` | Full JSON Schema describing `inputSchema` |
| `ProtocolTool` | `Tool` | Underlying protocol `Tool` object |

> ⚠️ The property is **`JsonSchema`**, not `InputSchema`.

Parsing the input schema:
```csharp
var schema = tool.JsonSchema;
schema.TryGetProperty("properties", out var props);  // object
schema.TryGetProperty("required", out var req);       // string[]
// Each prop.Value has: "type", "description", "enum"
```

---

## CallToolResult

**Class** — `ModelContextProtocol.Protocol.CallToolResult`

Returned by `McpClient.CallToolAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `Content` | `IList<ContentBlock>` | The tool's response content |
| `IsError` | `bool?` | `true` if the tool reported an error |
| `StructuredContent` | `JsonElement?` | Optional structured JSON result |

Extracting text:
```csharp
var text = string.Join("\n\n", result.Content
    .OfType<TextContentBlock>()
    .Select(c => c.Text));
```

---

## ContentBlock (abstract) / TextContentBlock

**`ModelContextProtocol.Protocol`**

`ContentBlock` is an abstract base. Use `.OfType<T>()` to get concrete types.

| Type | `Type` string | Key property |
|------|---------------|-------------|
| `TextContentBlock` | `"text"` | `Text` (`string`, required) |
| `ImageContentBlock` | `"image"` | `Data` (base64), `MimeType` |
| `ResourceContentBlock` | `"resource"` | `Resource` |

---

## HttpClientTransport

**Class** — `ModelContextProtocol.Client.HttpClientTransport`
Implements `IClientTransport`. Supports SSE and Streamable HTTP.

```csharp
// With auth-injected HttpClient:
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var transport = new HttpClientTransport(
    new HttpClientTransportOptions { Endpoint = new Uri("http://localhost:5000/mcp") },
    httpClient,
    ownsHttpClient: true);
```

`HttpClientTransportOptions`:
- `Endpoint` (`Uri`) — the MCP server endpoint URL

---

## Full client creation pattern (with token passthrough)

```csharp
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", bearerToken);

var transport = new HttpClientTransport(
    new HttpClientTransportOptions { Endpoint = new Uri(endpoint) },
    httpClient,
    ownsHttpClient: true);

McpClient client = await McpClient.CreateAsync(transport, cancellationToken: ct);

// List tools
IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: ct);

// Call a tool
CallToolResult result = await client.CallToolAsync(
    "cases_search",
    new Dictionary<string, object?> { ["query"] = "Smith", ["page"] = 1 },
    cancellationToken: ct);

string text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
```

---

## Notes

- `McpClientFactory` (static factory class) was **removed** in 1.0.0-rc.1. Use `McpClient.CreateAsync()` instead.
- `IMcpClient` interface was **removed**; use the concrete `McpClient` class.
- `SseClientTransport` was **renamed** to `HttpClientTransport`.
- `CallToolResponse` was **renamed** to `CallToolResult`.
