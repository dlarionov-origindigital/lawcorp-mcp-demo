# BUG-001: Sign-in link returns 404 when UseAuth=false

**Status:** FIXED
**Type:** Bug
**Feature:** [8.6: Header Links](./8.6-header-links.md)
**Severity:** High
**Tags:** `+web-app` `+auth` `+bug`

---

## Summary

Clicking the "Sign in" button in the header navigates to `/MicrosoftIdentity/Account/SignIn` which returns a 404 Not Found page.

## Steps to Reproduce

1. Set `UseAuth: false` in `appsettings.Development.json` (or use the committed default)
2. Run the web app: `dotnet run --project src/LawCorp.Mcp.Web`
3. Navigate to `https://localhost:5001`
4. Observe the "Sign in" button in the header
5. Click "Sign in"
6. **Result:** 404 Not Found page

## Expected Behaviour

Either:
- (a) The sign-in link is not shown when auth is disabled, OR
- (b) The link navigates to the Entra ID login page

## Root Cause

`LoginDisplay.razor` always renders the `<AuthorizeView><NotAuthorized>` block containing a link to `/MicrosoftIdentity/Account/SignIn`. However, when `UseAuth=false`, `Program.cs` skips:

1. `builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI()` — so the `AccountController` is never registered
2. `app.MapControllers()` — so no controller routes are mapped

The `/MicrosoftIdentity/Account/SignIn` route simply does not exist, resulting in 404.

## Fix Applied

**`LoginDisplay.razor`:** Added a `UseAuth` config check. When `false`, renders a "Demo mode" badge instead of the sign-in link. When `true`, renders the sign-in/sign-out UI as before.

**`Home.razor`:** Added the same `UseAuth` guard around the `<AuthorizeView>` block. When `false`, shows an informational banner explaining how to enable auth.

## Affected Files

- `src/LawCorp.Mcp.Web/Components/Layout/LoginDisplay.razor`
- `src/LawCorp.Mcp.Web/Components/Pages/Home.razor`

## Verification

- With `UseAuth=false`: header shows "Demo mode" badge, no sign-in link, no 404 possible
- With `UseAuth=true`: header shows "Sign in" button, clicking it initiates OIDC redirect
