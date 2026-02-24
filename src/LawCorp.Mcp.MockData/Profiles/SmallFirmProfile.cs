namespace LawCorp.Mcp.MockData.Profiles;

/// <summary>Small profile: ~20 attorneys, ~30 cases. Useful for fast iteration and unit tests.</summary>
public class SmallFirmProfile : IFirmProfile
{
    public int AttorneyCount => 20;
    public int ClientCount => 15;
    public int CaseCount => 30;
    public int DocumentsPerCase => 3;
    public int TimeEntriesPerCase => 8;
    public int HearingsPerCase => 1;
    public int DeadlinesPerCase => 2;
    public int ResearchMemosPerCase => 1;
}
