namespace LawCorp.Mcp.Tests.E2E.Fixtures;

/// <summary>
/// Reads persona credentials from environment variables.
/// Each persona needs EMAIL and PASSWORD env vars set.
/// </summary>
public static class PersonaCredentials
{
    public static (string Email, string Password) HarveySpecter => Read("HARVEY");
    public static (string Email, string Password) KimWexler => Read("KIM");
    public static (string Email, string Password) AlanShore => Read("ALAN");
    public static (string Email, string Password) ErinBrockovich => Read("ERIN");
    public static (string Email, string Password) ElleWoods => Read("ELLE");
    public static (string Email, string Password) VinnyGambini => Read("VINNY");

    public static IEnumerable<(string Name, string Email, string Password)> All()
    {
        yield return ("Harvey Specter", HarveySpecter.Email, HarveySpecter.Password);
        yield return ("Kim Wexler", KimWexler.Email, KimWexler.Password);
        yield return ("Alan Shore", AlanShore.Email, AlanShore.Password);
        yield return ("Erin Brockovich", ErinBrockovich.Email, ErinBrockovich.Password);
        yield return ("Elle Woods", ElleWoods.Email, ElleWoods.Password);
        yield return ("Vinny Gambini", VinnyGambini.Email, VinnyGambini.Password);
    }

    /// <summary>
    /// Returns a single configured persona for smoke-testing login.
    /// Prefers Harvey but falls back to the first persona that has credentials set.
    /// </summary>
    public static (string Name, string Email, string Password)? AnyConfigured()
    {
        foreach (var persona in All())
        {
            if (!string.IsNullOrEmpty(persona.Email) && !string.IsNullOrEmpty(persona.Password))
                return persona;
        }
        return null;
    }

    private static (string Email, string Password) Read(string prefix)
    {
        var email = Environment.GetEnvironmentVariable($"E2E_{prefix}_EMAIL") ?? string.Empty;
        var password = Environment.GetEnvironmentVariable($"E2E_{prefix}_PASSWORD") ?? string.Empty;
        return (email, password);
    }
}
