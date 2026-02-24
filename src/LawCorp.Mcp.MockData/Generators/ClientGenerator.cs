using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.MockData.Partials;

namespace LawCorp.Mcp.MockData.Generators;

public class ClientGenerator(Random rng)
{
    private static readonly string[] Industries =
        ["Technology", "Finance", "Healthcare", "Energy", "Manufacturing", "Retail", "Media", "Real Estate"];

    public Client Generate(int id)
    {
        var name = CompanyNames.Names[rng.Next(CompanyNames.Names.Length)];
        var suffix = CompanyNames.Suffixes[rng.Next(CompanyNames.Suffixes.Length)];

        return new Client
        {
            Id = id,
            Name = $"{name} {suffix}",
            Type = ClientType.Organization,
            Industry = Industries[rng.Next(Industries.Length)],
            ContactEmail = $"legal@{name.ToLower().Replace(" ", "")}.com",
            ContactPhone = $"({rng.Next(200, 999)}) {rng.Next(200, 999)}-{rng.Next(1000, 9999)}",
            Address = "123 Corporate Blvd, Wilmington, DE 19801",
            EngagementDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-rng.Next(1, 10))),
            Status = "Active"
        };
    }

    public IEnumerable<Client> GenerateMany(int count)
    {
        for (var i = 1; i <= count; i++)
            yield return Generate(i);
    }
}
