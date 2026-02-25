using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.MockData.Partials;

namespace LawCorp.Mcp.MockData.Generators;

/// <summary>
/// Generates random attorney <see cref="User"/> records with accompanying
/// <see cref="AttorneyDetails"/>. The caller is responsible for persisting
/// the details after the users are saved (to get their IDs).
/// </summary>
public class AttorneyGenerator(Random rng)
{
    private static readonly decimal[] PartnerRates = [650m, 750m, 850m, 950m, 1050m];
    private static readonly decimal[] AssociateRates = [300m, 350m, 400m, 450m, 500m];
    private static readonly decimal[] OfCounselRates = [450m, 500m, 550m, 600m];

    public (User User, AttorneyDetails Details) Generate(int sequenceNumber, int practiceGroupId)
    {
        var role = sequenceNumber <= 10 ? FirmRole.Partner
            : sequenceNumber <= 60 ? FirmRole.Associate
            : FirmRole.OfCounsel;

        var firstName = FirstNames.Attorney[rng.Next(FirstNames.Attorney.Length)];
        var lastName = LastNames.Attorney[rng.Next(LastNames.Attorney.Length)];
        var rates = role == FirmRole.Partner ? PartnerRates
            : role == FirmRole.Associate ? AssociateRates
            : OfCounselRates;

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@lawcorp.com",
            FirmRole = role,
            PracticeGroupId = practiceGroupId,
            HireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-rng.Next(1, 20))),
            IsActive = true
        };

        var details = new AttorneyDetails
        {
            BarNumber = $"{rng.Next(100000, 999999)}",
            HourlyRate = rates[rng.Next(rates.Length)]
        };

        return (user, details);
    }

    public IEnumerable<(User User, AttorneyDetails Details)> GenerateMany(int count, IList<PracticeGroup> practiceGroups)
    {
        for (var i = 1; i <= count; i++)
        {
            var group = practiceGroups[rng.Next(practiceGroups.Count)];
            yield return Generate(i, group.Id);
        }
    }
}
