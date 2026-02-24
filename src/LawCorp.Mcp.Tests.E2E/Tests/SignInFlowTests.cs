using LawCorp.Mcp.Tests.E2E.Auth;
using LawCorp.Mcp.Tests.E2E.Fixtures;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace LawCorp.Mcp.Tests.E2E.Tests;

/// <summary>
/// Validates the core sign-in user flow: navigate → Entra ID redirect → credentials →
/// redirect back → identity displayed in UI. Uses a random available persona.
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public class SignInFlowTests
{
    private readonly WebAppFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SignInFlowTests(WebAppFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task HomePage_Shows_SignInButton_When_Anonymous()
    {
        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(_fixture.BaseUrl);

        var signInButton = page.Locator("a[href='MicrosoftIdentity/Account/SignIn']");
        await signInButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        Assert.True(await signInButton.IsVisibleAsync());

        var infoBanner = page.Locator(".lc-info-banner");
        Assert.True(await infoBanner.IsVisibleAsync());

        await context.CloseAsync();
    }

    [Fact]
    public async Task SignIn_With_Persona_Shows_Identity_Card()
    {
        var persona = PersonaCredentials.AnyConfigured();
        if (persona is null)
        {
            _output.WriteLine("SKIPPED: No persona credentials configured. Set E2E_HARVEY_EMAIL / E2E_HARVEY_PASSWORD.");
            return;
        }

        var (name, email, password) = persona.Value;
        _output.WriteLine($"Testing sign-in with persona: {name} ({email})");

        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await EntraLoginHelper.LoginAsync(page, _fixture.BaseUrl, email, password);

        var identityCard = page.Locator(".lc-identity-card");
        await identityCard.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        Assert.True(await identityCard.IsVisibleAsync());

        var displayedName = await page.Locator(".lc-identity-card__name").TextContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(displayedName));
        _output.WriteLine($"Identity card shows: {displayedName}");

        await EntraLoginHelper.SaveStorageStateAsync(page, name);
        await context.CloseAsync();
    }

    [Fact]
    public async Task SignIn_Then_Claims_Page_Shows_Claims()
    {
        var persona = PersonaCredentials.AnyConfigured();
        if (persona is null)
        {
            _output.WriteLine("SKIPPED: No persona credentials configured.");
            return;
        }

        var (_, email, password) = persona.Value;

        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await EntraLoginHelper.LoginAsync(page, _fixture.BaseUrl, email, password);

        await page.GotoAsync($"{_fixture.BaseUrl}/account/claims");

        var claimsTable = page.Locator(".lc-claims-table");
        await claimsTable.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        var rows = page.Locator(".lc-claims-table tbody tr");
        var count = await rows.CountAsync();
        Assert.True(count > 0, "Expected at least one claim row after sign-in");
        _output.WriteLine($"Claims page shows {count} claims");

        await context.CloseAsync();
    }

    [Fact]
    public async Task SignOut_Returns_To_Anonymous_State()
    {
        var persona = PersonaCredentials.AnyConfigured();
        if (persona is null)
        {
            _output.WriteLine("SKIPPED: No persona credentials configured.");
            return;
        }

        var (_, email, password) = persona.Value;

        var context = await _fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await EntraLoginHelper.LoginAsync(page, _fixture.BaseUrl, email, password);

        await page.Locator("a[href='MicrosoftIdentity/Account/SignOut']").ClickAsync();

        await page.WaitForURLAsync(url =>
            url.StartsWith(_fixture.BaseUrl) || url.Contains("login.microsoftonline.com"),
            new PageWaitForURLOptions { Timeout = 15_000 });

        if (page.Url.StartsWith(_fixture.BaseUrl))
        {
            var signInButton = page.Locator("a[href='MicrosoftIdentity/Account/SignIn']");
            await signInButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
            Assert.True(await signInButton.IsVisibleAsync());
        }

        await context.CloseAsync();
    }
}
