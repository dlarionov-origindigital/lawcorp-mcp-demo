using LawCorp.Mcp.Core.Models;

namespace LawCorp.Mcp.MockData.Generators;

public class TimeEntryGenerator(Random rng)
{
    private static readonly string[] Descriptions =
    [
        "Review and analysis of merger agreement draft",
        "Client call regarding transaction timeline",
        "Due diligence document review",
        "Drafting representations and warranties",
        "Research on Delaware corporate law precedents",
        "Preparation of closing checklist",
        "Negotiation of indemnification provisions",
        "Review of regulatory filings",
        "Preparation of board resolution",
        "Conference call with opposing counsel"
    ];

    public TimeEntry Generate(User user, decimal hourlyRate, Case @case)
    {
        var date = @case.OpenDate.AddDays(rng.Next(0, 180));
        var hours = Math.Round(0.5 + rng.NextDouble() * (user.FirmRole == FirmRole.Associate ? 7.5 : 4.5), 1);

        return new TimeEntry
        {
            UserId = user.Id,
            CaseId = @case.Id,
            Date = date,
            Hours = (decimal)hours,
            Description = Descriptions[rng.Next(Descriptions.Length)],
            BillableRate = hourlyRate,
            Billable = rng.NextDouble() < 0.85,
            Status = TimeEntryStatus.Submitted
        };
    }
}
