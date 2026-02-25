namespace LawCorp.Mcp.Web;

/// <summary>
/// Configuration key constants matching <c>appsettings.json</c> structure.
/// Used with <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> indexers
/// and <c>GetValue</c>/<c>GetSection</c> calls.
/// </summary>
public static class AppConfigKeys
{
    public const string UseAuth = "UseAuth";

    public static class AzureAd
    {
        public const string Section = "AzureAd";
    }

    public static class McpServer
    {
        public const string Endpoint = "McpServer:Endpoint";
        public const string Scopes = "McpServer:Scopes";
        public const string DefaultEndpoint = "http://localhost:5000/mcp";
    }

    public static class Branding
    {
        public const string AppName = "Branding:AppName";
        public const string Tagline = "Branding:Tagline";
        public const string FooterText = "Branding:FooterText";

        public const string DefaultAppName = "Law-Corp";
        public const string DefaultTagline = "Corporate law. Corporately.";
        public const string DefaultFooterText = "Law-Corp LLP — MCP Reference Architecture";
    }
}
