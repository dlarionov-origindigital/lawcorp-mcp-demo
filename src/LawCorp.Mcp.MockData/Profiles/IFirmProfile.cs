namespace LawCorp.Mcp.MockData.Profiles;

public interface IFirmProfile
{
    int AttorneyCount { get; }
    int ClientCount { get; }
    int CaseCount { get; }
    int DocumentsPerCase { get; }
    int TimeEntriesPerCase { get; }
    int HearingsPerCase { get; }
    int DeadlinesPerCase { get; }
    int ResearchMemosPerCase { get; }
}
