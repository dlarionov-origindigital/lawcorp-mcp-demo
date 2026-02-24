using LawCorp.Mcp.Server;

// Ensure appsettings.json is found regardless of the caller's working directory.
// MCP clients (Claude Desktop, Inspector, VS Code) launch the exe as a subprocess
// whose CWD is not the bin directory. See bug 1.1.2.1.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var transport = ServerBootstrap.ResolveTransport(args);

if (transport.Equals("http", StringComparison.OrdinalIgnoreCase))
    await ServerBootstrap.RunHttpAsync(args);
else
    await ServerBootstrap.RunStdioAsync(args);
