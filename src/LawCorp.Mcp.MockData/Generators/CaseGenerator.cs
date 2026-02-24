using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.MockData.Partials;

namespace LawCorp.Mcp.MockData.Generators;

public class CaseGenerator(Random rng)
{
    public Case Generate(int id, IList<Client> clients, IList<PracticeGroup> practiceGroups, IList<Attorney> attorneys)
    {
        var client = clients[rng.Next(clients.Count)];
        var group = practiceGroups[rng.Next(practiceGroups.Count)];
        var template = CaseTitles.Templates[rng.Next(CaseTitles.Templates.Length)];
        var company1 = CompanyNames.Names[rng.Next(CompanyNames.Names.Length)];
        var company2 = CompanyNames.Names[rng.Next(CompanyNames.Names.Length)];

        var title = template
            .Replace("{Company}", company1, StringComparison.OrdinalIgnoreCase)
            .Replace("{Company}", company2, StringComparison.OrdinalIgnoreCase);

        var openDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-rng.Next(1, 5)).AddDays(-rng.Next(0, 365)));
        var isClosed = rng.NextDouble() < 0.3;

        var lead = attorneys
            .Where(a => a.PracticeGroupId == group.Id && a.Role == AttorneyRole.Partner)
            .MinBy(_ => rng.Next())
            ?? attorneys.First(a => a.Role == AttorneyRole.Partner);

        return new Case
        {
            Id = id,
            CaseNumber = $"LC-{DateTime.Today.Year}-{id:D4}",
            Title = title,
            Description = $"Matter involving {client.Name}. {template.Split('—').Last().Trim()}",
            Status = isClosed ? CaseStatus.Closed : CaseStatus.Active,
            PracticeGroupId = group.Id,
            ClientId = client.Id,
            OpenDate = openDate,
            CloseDate = isClosed ? openDate.AddMonths(rng.Next(6, 24)) : null,
            EstimatedValue = rng.Next(50, 2000) * 10000m
        };
    }

    public IEnumerable<Case> GenerateMany(int count, IList<Client> clients, IList<PracticeGroup> groups, IList<Attorney> attorneys)
    {
        for (var i = 1; i <= count; i++)
            yield return Generate(i, clients, groups, attorneys);
    }
}
