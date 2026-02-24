using LawCorp.Mcp.Data;

namespace LawCorp.Mcp.MockData.Personas;

/// <summary>
/// Seeds the six canonical personas into the database.
/// Tenant-specific Entra ID values come from <see cref="PersonaSeedConfig"/>
/// (loaded from <c>persona-seed.json</c>). If no config is provided, personas
/// are seeded with placeholder emails and no EntraObjectId.
/// <para>
/// Called by <see cref="MockDataSeeder"/> after practice groups are created but
/// before random attorneys, so persona attorneys get deterministic low IDs.
/// </para>
/// </summary>
public class PersonaSeeder(LawCorpDbContext db, PersonaSeedConfig? config = null)
{
    private readonly PersonaSeedConfig _config = config ?? new PersonaSeedConfig();

    public async Task<PersonaSeedResult> SeedAsync(CancellationToken ct = default)
    {
        var harvey = PersonaDefinitions.HarveySpecter(_config.HarveySpecter);
        var kim = PersonaDefinitions.KimWexler(_config.KimWexler);
        var alan = PersonaDefinitions.AlanShore(_config.AlanShore);

        await db.Attorneys.AddRangeAsync([harvey, kim, alan], ct);
        await db.SaveChangesAsync(ct);

        var erin = PersonaDefinitions.ErinBrockovich(_config.ErinBrockovich);
        await db.Paralegals.AddAsync(erin, ct);

        var elle = PersonaDefinitions.ElleWoods(_config.ElleWoods);
        elle.AssignedAttorneyId = harvey.Id;
        await db.LegalAssistants.AddAsync(elle, ct);

        var vinny = PersonaDefinitions.VinnyGambini(_config.VinnyGambini);
        vinny.SupervisorId = harvey.Id;
        await db.Interns.AddAsync(vinny, ct);

        await db.SaveChangesAsync(ct);

        return new PersonaSeedResult(harvey, kim, alan, erin, elle, vinny);
    }
}

public record PersonaSeedResult(
    Core.Models.Attorney Harvey,
    Core.Models.Attorney Kim,
    Core.Models.Attorney Alan,
    Core.Models.Paralegal Erin,
    Core.Models.LegalAssistant Elle,
    Core.Models.Intern Vinny);
