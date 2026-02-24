using LawCorp.Mcp.Tests.E2E.Fixtures;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace LawCorp.Mcp.Tests.E2E.Tests;

/// <summary>
/// Story 8.6.1: As a visitor on a page with the header visible in an unauthenticated
/// state, I should be able to click the sign-in link to go to the authentication page.
///
/// These tests validate that the header sign-in link is visible when UseAuth=true and
/// that clicking it initiates the OIDC redirect to Entra ID.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public class HeaderSignInLinkTests
{
    private readonly WebAppFixture _fixture;
    private readonly ITestOutputHelper _output;

    public HeaderSignInLinkTests(WebAppFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Header_Shows_SignIn_Button_When_Unauthenticated()
    {
        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_fixture.BaseUrl);

        var signInLink = page.Locator("a[href='MicrosoftIdentity/Account/SignIn']");
        var isVisible = await signInLink.IsVisibleAsync();

        if (isVisible)
        {
            _output.WriteLine("Sign-in link is visible in header (UseAuth=true)");
            Assert.True(isVisible);
        }
        else
        {
            var demoBadge = page.Locator("text=Demo mode");
            var demoVisible = await demoBadge.IsVisibleAsync();
            _output.WriteLine($"Sign-in link hidden; Demo mode badge visible: {demoVisible} (UseAuth=false)");
            Assert.True(demoVisible, "When sign-in link is hidden, 'Demo mode' badge should be visible");
        }

        await context.CloseAsync();
    }

    [Fact]
    public async Task SignIn_Link_Does_Not_Return_404()
    {
        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_fixture.BaseUrl);

        var signInLink = page.Locator("a[href='MicrosoftIdentity/Account/SignIn']");
        if (!await signInLink.IsVisibleAsync())
        {
            _output.WriteLine("SKIPPED: UseAuth=false, sign-in link not rendered (no 404 possible)");
            await context.CloseAsync();
            return;
        }

        var responseTask = page.WaitForResponseAsync(
            resp => resp.Url.Contains("MicrosoftIdentity/Account/SignIn") ||
                    resp.Url.Contains("login.microsoftonline.com"));

        await signInLink.ClickAsync();

        var response = await responseTask;
        _output.WriteLine($"Navigation response: {response.Status} → {response.Url}");

        Assert.NotEqual(404, response.Status);

        await context.CloseAsync();
    }

    [Fact]
    public async Task SignIn_Link_Click_Redirects_To_Entra_Login()
    {
        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_fixture.BaseUrl);

        var signInLink = page.Locator("a[href='MicrosoftIdentity/Account/SignIn']");
        if (!await signInLink.IsVisibleAsync())
        {
            _output.WriteLine("SKIPPED: UseAuth=false, sign-in link not rendered");
            await context.CloseAsync();
            return;
        }

        await signInLink.ClickAsync();

        try
        {
            await page.WaitForURLAsync(
                url => url.Contains("login.microsoftonline.com"),
                new PageWaitForURLOptions { Timeout = 15_000 });

            _output.WriteLine($"Redirected to Entra ID: {page.Url}");
            Assert.Contains("login.microsoftonline.com", page.Url);
        }
        catch (TimeoutException)
        {
            _output.WriteLine($"Did not redirect to Entra ID. Current URL: {page.Url}");
            Assert.Fail($"Expected redirect to login.microsoftonline.com but landed on: {page.Url}");
        }

        await context.CloseAsync();
    }

    [Fact]
    public async Task Header_SignIn_Visible_From_Every_Page()
    {
        var pages = new[] { "/", "/tools", "/resources", "/prompts", "/account/claims" };

        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        foreach (var path in pages)
        {
            await page.GotoAsync($"{_fixture.BaseUrl}{path}");

            var signInLink = page.Locator("a[href='MicrosoftIdentity/Account/SignIn']");
            var demoBadge = page.Locator("text=Demo mode");

            var signInVisible = await signInLink.IsVisibleAsync();
            var demoVisible = await demoBadge.IsVisibleAsync();

            _output.WriteLine($"{path}: sign-in={signInVisible}, demo={demoVisible}");
            Assert.True(signInVisible || demoVisible,
                $"Page {path}: neither sign-in link nor demo badge is visible in header");
        }

        await context.CloseAsync();
    }
}
