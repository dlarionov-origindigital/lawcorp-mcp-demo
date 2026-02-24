namespace LawCorp.Mcp.MockData.Profiles;

/// <summary>Large profile: ~200 attorneys, ~500 cases. For load testing and full-scale demos.</summary>
public class LargeFirmProfile : IFirmProfile
{
    public int AttorneyCount => 200;
    public int ClientCount => 150;
    public int CaseCount => 500;
    public int DocumentsPerCase => 6;
    public int TimeEntriesPerCase => 20;
    public int HearingsPerCase => 3;
    public int DeadlinesPerCase => 5;
    public int ResearchMemosPerCase => 2;
}
