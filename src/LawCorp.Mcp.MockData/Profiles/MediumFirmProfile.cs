namespace LawCorp.Mcp.MockData.Profiles;

/// <summary>Default profile: ~80 attorneys, ~150 cases. Used for standard development and demos.</summary>
public class MediumFirmProfile : IFirmProfile
{
    public int AttorneyCount => 80;
    public int ClientCount => 60;
    public int CaseCount => 150;
    public int DocumentsPerCase => 4;
    public int TimeEntriesPerCase => 12;
    public int HearingsPerCase => 2;
    public int DeadlinesPerCase => 3;
    public int ResearchMemosPerCase => 1;
}
