using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Data;

namespace LawCorp.Mcp.MockData.Personas;

/// <summary>
/// Seeds the six canonical personas into the database as <see cref="User"/> records
/// with their role-specific satellite data (<see cref="AttorneyDetails"/>,
/// <see cref="InternDetails"/>).
/// </summary>
public class PersonaSeeder(LawCorpDbContext db, PersonaSeedConfig? config = null)
{
    private readonly PersonaSeedConfig _config = config ?? new PersonaSeedConfig();

    public async Task<PersonaSeedResult> SeedAsync(CancellationToken ct = default)
    {
        // Attorneys
        var harvey = PersonaDefinitions.HarveySpecter(_config.HarveySpecter);
        var kim = PersonaDefinitions.KimWexler(_config.KimWexler);
        var alan = PersonaDefinitions.AlanShore(_config.AlanShore);

        await db.Users.AddRangeAsync([harvey, kim, alan], ct);
        await db.SaveChangesAsync(ct);

        // Attorney details (need User IDs from above)
        var harveyDetails = PersonaDefinitions.HarveySpecterDetails();
        harveyDetails.UserId = harvey.Id;
        var kimDetails = PersonaDefinitions.KimWexlerDetails();
        kimDetails.UserId = kim.Id;
        var alanDetails = PersonaDefinitions.AlanShoreDetails();
        alanDetails.UserId = alan.Id;

        await db.AttorneyDetails.AddRangeAsync([harveyDetails, kimDetails, alanDetails], ct);
        await db.SaveChangesAsync(ct);

        // Staff (with SupervisorId pointing to Harvey)
        var erin = PersonaDefinitions.ErinBrockovich(_config.ErinBrockovich);
        await db.Users.AddAsync(erin, ct);

        var elle = PersonaDefinitions.ElleWoods(_config.ElleWoods);
        elle.SupervisorId = harvey.Id;
        await db.Users.AddAsync(elle, ct);

        var vinny = PersonaDefinitions.VinnyGambini(_config.VinnyGambini);
        vinny.SupervisorId = harvey.Id;
        await db.Users.AddAsync(vinny, ct);
        await db.SaveChangesAsync(ct);

        // Intern details
        var vinnyDetails = PersonaDefinitions.VinnyGambiniDetails();
        vinnyDetails.UserId = vinny.Id;
        await db.InternDetails.AddAsync(vinnyDetails, ct);
        await db.SaveChangesAsync(ct);

        return new PersonaSeedResult(harvey, kim, alan, erin, elle, vinny);
    }
}

public record PersonaSeedResult(
    User Harvey,
    User Kim,
    User Alan,
    User Erin,
    User Elle,
    User Vinny);
