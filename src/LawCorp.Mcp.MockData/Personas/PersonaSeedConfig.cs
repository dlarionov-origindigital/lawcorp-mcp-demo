namespace LawCorp.Mcp.MockData.Personas;

/// <summary>
/// Configuration model bound from the <c>PersonaSeed</c> section of
/// <c>persona-seed.json</c>. Contains only tenant-specific Entra ID values
/// (email and Object ID) — persona structure (names, roles, practice groups)
/// lives in <see cref="PersonaDefinitions"/>.
/// <para>
/// Copy <c>persona-seed.json.example</c> → <c>persona-seed.json</c> and
/// fill in your tenant's values. The file is gitignored.
/// </para>
/// </summary>
public class PersonaSeedConfig
{
    public PersonaIdentity HarveySpecter { get; set; } = new();
    public PersonaIdentity KimWexler { get; set; } = new();
    public PersonaIdentity AlanShore { get; set; } = new();
    public PersonaIdentity ErinBrockovich { get; set; } = new();
    public PersonaIdentity ElleWoods { get; set; } = new();
    public PersonaIdentity VinnyGambini { get; set; } = new();
}

public class PersonaIdentity
{
    public string Email { get; set; } = string.Empty;
    public string EntraObjectId { get; set; } = string.Empty;
}
