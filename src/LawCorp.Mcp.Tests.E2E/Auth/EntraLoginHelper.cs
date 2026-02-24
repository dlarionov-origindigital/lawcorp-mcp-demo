using Microsoft.Playwright;

namespace LawCorp.Mcp.Tests.E2E.Auth;

/// <summary>
/// Automates the Entra ID (Azure AD) browser-based login flow.
/// Fills in email → clicks Next → fills password → clicks Sign in → handles
/// "Stay signed in?" prompt → waits for redirect back to the Blazor app.
/// </summary>
public static class EntraLoginHelper
{
    public static async Task LoginAsync(IPage page, string appUrl, string email, string password)
    {
        await page.GotoAsync($"{appUrl}/MicrosoftIdentity/Account/SignIn");

        await page.WaitForURLAsync(url => url.Contains("login.microsoftonline.com"), new PageWaitForURLOptions
        {
            Timeout = 15_000
        });

        var emailInput = page.Locator("input[type='email']");
        await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        await emailInput.FillAsync(email);
        await page.Locator("input[type='submit']").ClickAsync();

        var passwordInput = page.Locator("input[type='password']");
        await passwordInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        await passwordInput.FillAsync(password);
        await page.Locator("input[type='submit']").ClickAsync();

        var staySignedIn = page.Locator("input#idSIButton9");
        try
        {
            await staySignedIn.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
            await staySignedIn.ClickAsync();
        }
        catch (TimeoutException)
        {
            // "Stay signed in?" prompt did not appear — proceed
        }

        await page.WaitForURLAsync(url => url.StartsWith(appUrl), new PageWaitForURLOptions
        {
            Timeout = 15_000
        });
    }

    public static async Task SaveStorageStateAsync(IPage page, string personaName)
    {
        var authDir = Path.Combine(AppContext.BaseDirectory, ".auth");
        Directory.CreateDirectory(authDir);
        var path = Path.Combine(authDir, $"{personaName.ToLowerInvariant().Replace(' ', '-')}.json");
        await page.Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path });
    }

    public static string GetStorageStatePath(string personaName)
    {
        return Path.Combine(AppContext.BaseDirectory, ".auth",
            $"{personaName.ToLowerInvariant().Replace(' ', '-')}.json");
    }
}
