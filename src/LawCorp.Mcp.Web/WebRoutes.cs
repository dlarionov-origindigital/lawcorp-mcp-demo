namespace LawCorp.Mcp.Web;

/// <summary>
/// Route path constants for the Law-Corp web app.
/// Used in NavMenu links, page hyperlinks, and <c>NavigationManager.NavigateTo</c> calls.
/// <para>
/// Note: Blazor <c>@page</c> directives require string literals and cannot reference these
/// constants. When adding a new route, define the constant here <b>and</b> set the matching
/// <c>@page</c> directive in the component.
/// </para>
/// </summary>
public static class WebRoutes
{
    public const string Home = "/";
    public const string Tools = "/tools";
    public const string Resources = "/resources";
    public const string Prompts = "/prompts";
    public const string Error = "/Error";
    public const string Trace = "/trace";
    public const string Audit = "/audit";

    public static class Account
    {
        public const string Claims = "/account/claims";
    }

    public static class Auth
    {
        public const string SignIn = "MicrosoftIdentity/Account/SignIn";
        public const string SignOut = "MicrosoftIdentity/Account/SignOut";
    }
}
