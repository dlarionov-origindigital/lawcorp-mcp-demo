using MediatR;

namespace LawCorp.Mcp.Core.Queries;

public record GetCaseByIdQuery(int CaseId) : IRequest<GetCaseResult>;

public record CaseTeamMember(string Name, string Email, string Role, string Since);
public record CasePartyInfo(string Name, string Role, string? Representation);
public record CaseCourtInfo(string Name, string Jurisdiction);
public record CaseClientInfo(int Id, string Name, string Type, string Industry);

public record CaseDetail(
    int Id,
    string CaseNumber,
    string Title,
    string Description,
    string Status,
    string PracticeGroup,
    CaseClientInfo Client,
    CaseCourtInfo? Court,
    string OpenDate,
    string? CloseDate,
    decimal EstimatedValue,
    IReadOnlyList<CaseTeamMember> Team,
    IReadOnlyList<CasePartyInfo> Parties);

public record GetCaseResult(CaseDetail? Case, string? Error = null);
