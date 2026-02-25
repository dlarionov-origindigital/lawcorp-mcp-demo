using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.MockData.Personas;

/// <summary>
/// Canonical persona fixture for the Law-Corp MCP server.
/// Persona structure (names, roles, practice groups) is fixed in code;
/// tenant-specific Entra ID values (email, OID) come from
/// <see cref="PersonaSeedConfig"/> loaded from <c>persona-seed.json</c>.
/// </summary>
public static class PersonaDefinitions
{
    // ── Attorneys ─────────────────────────────────────────────────────────────

    public static User HarveySpecter(PersonaIdentity id) => new()
    {
        FirstName = "Harvey",
        LastName = "Specter",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        FirmRole = FirmRole.Partner,
        PracticeGroupId = 1,
        HireDate = new DateOnly(2010, 3, 15),
        IsActive = true
    };

    public static AttorneyDetails HarveySpecterDetails() => new()
    {
        BarNumber = "NY-000001",
        HourlyRate = 950m
    };

    public static User KimWexler(PersonaIdentity id) => new()
    {
        FirstName = "Kim",
        LastName = "Wexler",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        FirmRole = FirmRole.Associate,
        PracticeGroupId = 5,
        HireDate = new DateOnly(2018, 6, 1),
        IsActive = true
    };

    public static AttorneyDetails KimWexlerDetails() => new()
    {
        BarNumber = "NM-000002",
        HourlyRate = 450m
    };

    public static User AlanShore(PersonaIdentity id) => new()
    {
        FirstName = "Alan",
        LastName = "Shore",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        FirmRole = FirmRole.OfCounsel,
        PracticeGroupId = 3,
        HireDate = new DateOnly(2015, 1, 10),
        IsActive = true
    };

    public static AttorneyDetails AlanShoreDetails() => new()
    {
        BarNumber = "MA-000003",
        HourlyRate = 550m
    };

    // ── Staff ─────────────────────────────────────────────────────────────────

    public static User ErinBrockovich(PersonaIdentity id) => new()
    {
        FirstName = "Erin",
        LastName = "Brockovich",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        FirmRole = FirmRole.Paralegal,
        PracticeGroupId = 1,
        HireDate = new DateOnly(2019, 9, 1),
        IsActive = true
    };

    public static User ElleWoods(PersonaIdentity id) => new()
    {
        FirstName = "Elle",
        LastName = "Woods",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        FirmRole = FirmRole.LegalAssistant,
        HireDate = new DateOnly(2021, 1, 15),
        IsActive = true
        // SupervisorId set at seed time after Harvey is persisted
    };

    public static User VinnyGambini(PersonaIdentity id) => new()
    {
        FirstName = "Vinny",
        LastName = "Gambini",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        FirmRole = FirmRole.Intern,
        PracticeGroupId = 1,
        HireDate = new DateOnly(2025, 6, 1),
        IsActive = true
        // SupervisorId set at seed time after Harvey is persisted
    };

    public static InternDetails VinnyGambiniDetails() => new()
    {
        School = "Brooklyn Law School",
        StartDate = new DateOnly(2025, 6, 1),
        EndDate = new DateOnly(2025, 12, 31)
    };

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
