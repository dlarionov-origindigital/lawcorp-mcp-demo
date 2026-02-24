# LawCorp.Mcp.Tests.E2E

Playwright end-to-end tests for the Law-Corp Blazor web app. These tests automate the Entra ID browser login flow using real persona credentials and validate that the sign-in, identity display, and claims features work correctly.

## Prerequisites

1. **Install Playwright browsers** (first time only):

```powershell
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install
```

2. **Configure persona credentials** — copy `.env.example` to `.env` and fill in at least one persona's email and password. Then load them into your shell:

```powershell
# PowerShell
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.*)$') {
        [Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim(), 'Process')
    }
}
```

3. **Entra ID test accounts** must have MFA disabled (or use a conditional access policy exclusion for test accounts). See `docs/auth-config.md` for account setup.

## Running Tests

```bash
# Run only E2E tests (they are excluded from default `dotnet test` by trait filter)
dotnet test --filter "Category=E2E"

# Run with visible browser for debugging
E2E_HEADLESS=false dotnet test --filter "Category=E2E"
```

## Project Structure

```
Auth/
  EntraLoginHelper.cs       ← Automates Entra ID browser login flow
Fixtures/
  WebAppFixture.cs          ← Hosts Blazor app in-process, shared Playwright browser
  PersonaCredentials.cs     ← Reads persona credentials from env vars
Tests/
  SignInFlowTests.cs        ← Core sign-in / sign-out / claims E2E tests
.auth/                      ← (gitignored) Cached storage state per persona
.env.example                ← Template for persona credential env vars
```

## Test Categories

All tests are tagged `[Trait("Category", "E2E")]` so they can be excluded from CI unit test runs:

```bash
# Run everything EXCEPT E2E
dotnet test --filter "Category!=E2E"
```
