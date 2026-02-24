using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.MockData.Personas;

/// <summary>
/// Canonical persona fixture for the Law-Corp MCP server.
/// Persona structure (names, roles, practice groups) is fixed in code;
/// tenant-specific Entra ID values (email, OID) come from
/// <see cref="PersonaSeedConfig"/> which is loaded from <c>persona-seed.json</c>.
/// </summary>
public static class PersonaDefinitions
{
    // ── Attorneys ─────────────────────────────────────────────────────────────

    public static Attorney HarveySpecter(PersonaIdentity id) => new()
    {
        FirstName = "Harvey",
        LastName = "Specter",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        BarNumber = "NY-000001",
        Role = AttorneyRole.Partner,
        PracticeGroupId = 1, // Mergers & Acquisitions
        HourlyRate = 950m,
        HireDate = new DateOnly(2010, 3, 15),
        IsActive = true
    };

    public static Attorney KimWexler(PersonaIdentity id) => new()
    {
        FirstName = "Kim",
        LastName = "Wexler",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        BarNumber = "NM-000002",
        Role = AttorneyRole.Associate,
        PracticeGroupId = 5, // Contract Law
        HourlyRate = 450m,
        HireDate = new DateOnly(2018, 6, 1),
        IsActive = true
    };

    public static Attorney AlanShore(PersonaIdentity id) => new()
    {
        FirstName = "Alan",
        LastName = "Shore",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        BarNumber = "MA-000003",
        Role = AttorneyRole.OfCounsel,
        PracticeGroupId = 3, // Securities & Compliance
        HourlyRate = 550m,
        HireDate = new DateOnly(2015, 1, 10),
        IsActive = true
    };

    // ── Staff ─────────────────────────────────────────────────────────────────

    public static Paralegal ErinBrockovich(PersonaIdentity id) => new()
    {
        FirstName = "Erin",
        LastName = "Brockovich",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        PracticeGroupId = 1, // Mergers & Acquisitions (works with Harvey)
        HireDate = new DateOnly(2019, 9, 1)
    };

    public static LegalAssistant ElleWoods(PersonaIdentity id) => new()
    {
        FirstName = "Elle",
        LastName = "Woods",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        // AssignedAttorneyId set at seed time after Harvey is persisted
        HireDate = new DateOnly(2021, 1, 15)
    };

    public static Intern VinnyGambini(PersonaIdentity id) => new()
    {
        FirstName = "Vinny",
        LastName = "Gambini",
        Email = id.Email,
        EntraObjectId = NullIfEmpty(id.EntraObjectId),
        School = "Brooklyn Law School",
        PracticeGroupId = 1, // Mergers & Acquisitions
        // SupervisorId set at seed time after Harvey is persisted
        StartDate = new DateOnly(2025, 6, 1),
        EndDate = new DateOnly(2025, 12, 31)
    };

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
