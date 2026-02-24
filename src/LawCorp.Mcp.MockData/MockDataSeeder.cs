using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.Data;
using LawCorp.Mcp.MockData.Generators;
using LawCorp.Mcp.MockData.Partials;
using LawCorp.Mcp.MockData.Profiles;
using Microsoft.EntityFrameworkCore;

namespace LawCorp.Mcp.MockData;

/// <summary>
/// Orchestrates deterministic mock data generation and seeds the LawCorpDbContext.
/// Use <see cref="SmallFirmProfile"/> for unit tests, <see cref="MediumFirmProfile"/> for dev, <see cref="LargeFirmProfile"/> for load testing.
/// </summary>
public class MockDataSeeder(LawCorpDbContext db, IFirmProfile? profile = null, int seed = 42)
{
    private readonly IFirmProfile _profile = profile ?? new MediumFirmProfile();
    private readonly Random _rng = new(seed);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Attorneys.AnyAsync(ct))
            return; // Already seeded — idempotent

        // 1. Practice groups (static reference data)
        var practiceGroups = SeedPracticeGroups();
        await db.PracticeGroups.AddRangeAsync(practiceGroups, ct);
        await db.SaveChangesAsync(ct);

        // 2. Courts (static reference data)
        var courts = SeedCourts();
        await db.Courts.AddRangeAsync(courts, ct);
        await db.SaveChangesAsync(ct);

        // 3. Attorneys
        var attorneyGen = new AttorneyGenerator(_rng);
        var attorneys = attorneyGen.GenerateMany(_profile.AttorneyCount, practiceGroups).ToList();
        await db.Attorneys.AddRangeAsync(attorneys, ct);
        await db.SaveChangesAsync(ct);

        // 4. Clients
        var clientGen = new ClientGenerator(_rng);
        var clients = clientGen.GenerateMany(_profile.ClientCount).ToList();
        await db.Clients.AddRangeAsync(clients, ct);
        await db.SaveChangesAsync(ct);

        // 5. Cases + related data
        var caseGen = new CaseGenerator(_rng);
        var docGen = new DocumentGenerator(_rng);
        var calGen = new CalendarGenerator(_rng);
        var researchGen = new ResearchGenerator(_rng);

        var cases = caseGen.GenerateMany(_profile.CaseCount, clients, practiceGroups, attorneys).ToList();
        await db.Cases.AddRangeAsync(cases, ct);
        await db.SaveChangesAsync(ct);

        foreach (var @case in cases)
        {
            var caseAttorneys = attorneys.Where(a => a.PracticeGroupId == @case.PracticeGroupId).ToList();
            if (caseAttorneys.Count == 0) caseAttorneys = attorneys;

            // Assignments
            var lead = caseAttorneys[_rng.Next(caseAttorneys.Count)];
            await db.CaseAssignments.AddAsync(new CaseAssignment
            {
                CaseId = @case.Id, AttorneyId = lead.Id, Role = AssignmentRole.Lead,
                AssignedDate = @case.OpenDate
            }, ct);

            // Documents
            var docs = docGen.GenerateForCase(@case, caseAttorneys, _profile.DocumentsPerCase).ToList();
            await db.Documents.AddRangeAsync(docs, ct);

            // Hearings
            var court = courts[_rng.Next(courts.Count)];
            for (var i = 0; i < _profile.HearingsPerCase; i++)
                await db.Hearings.AddAsync(calGen.GenerateHearing(@case, court), ct);

            // Deadlines
            for (var i = 0; i < _profile.DeadlinesPerCase; i++)
                await db.Deadlines.AddAsync(calGen.GenerateDeadline(@case, lead), ct);

            // Research memos
            for (var i = 0; i < _profile.ResearchMemosPerCase; i++)
                await db.ResearchMemos.AddAsync(researchGen.Generate(@case, lead), ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static List<PracticeGroup> SeedPracticeGroups() =>
    [
        new() { Id = 1, Name = "Mergers & Acquisitions", Description = "End-to-end M&A transaction support including buy-side, sell-side, and merger of equals." },
        new() { Id = 2, Name = "Corporate Governance", Description = "Board advisory, shareholder relations, and corporate charter matters." },
        new() { Id = 3, Name = "Securities & Compliance", Description = "SEC filings, public company compliance, and regulatory matters." },
        new() { Id = 4, Name = "Due Diligence & Investigations", Description = "Pre-transaction due diligence and internal investigations." },
        new() { Id = 5, Name = "Contract Law", Description = "Complex commercial contract drafting, review, and negotiation." },
        new() { Id = 6, Name = "Intellectual Property (Transactional)", Description = "IP licensing, transfers, and IP aspects of M&A transactions." }
    ];

    private static List<Court> SeedCourts() =>
    [
        new() { Id = 1, Name = "Delaware Court of Chancery", Jurisdiction = "Delaware", Address = "34 The Circle, Dover, DE 19901", Type = "State" },
        new() { Id = 2, Name = "U.S. District Court, S.D.N.Y.", Jurisdiction = "Federal/SDNY", Address = "500 Pearl St, New York, NY 10007", Type = "Federal" },
        new() { Id = 3, Name = "U.S. District Court, D. Del.", Jurisdiction = "Federal/Delaware", Address = "844 King St, Wilmington, DE 19801", Type = "Federal" },
        new() { Id = 4, Name = "New York Supreme Court, Commercial Division", Jurisdiction = "New York", Address = "60 Centre St, New York, NY 10007", Type = "State" },
        new() { Id = 5, Name = "California Superior Court, Los Angeles County", Jurisdiction = "California", Address = "111 N Hill St, Los Angeles, CA 90012", Type = "State" }
    ];
}
