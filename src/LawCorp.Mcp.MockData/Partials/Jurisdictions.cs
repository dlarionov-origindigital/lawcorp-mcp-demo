namespace LawCorp.Mcp.MockData.Partials;

/// <summary>Supported jurisdictions and court names for mock data.</summary>
public static class Jurisdictions
{
    public static readonly string[] Federal =
    [
        "U.S. District Court, S.D.N.Y.",
        "U.S. District Court, D. Del.",
        "U.S. District Court, N.D. Cal.",
        "U.S. Court of Appeals, Second Circuit",
        "U.S. Court of Appeals, Third Circuit",
        "U.S. Supreme Court"
    ];

    public static readonly string[] State =
    [
        "Delaware Court of Chancery",
        "New York Supreme Court, Commercial Division",
        "California Superior Court, Los Angeles County",
        "Texas District Court, Travis County",
        "Illinois Circuit Court, Cook County"
    ];

    public static readonly string[] All = [.. Federal, .. State];
}
