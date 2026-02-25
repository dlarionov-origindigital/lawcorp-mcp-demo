using MediatR;

namespace LawCorp.Mcp.Core.Queries;

public record SearchCasesQuery(
    string Query,
    string? Status = null,
    string? PracticeGroup = null,
    int? AssignedTo = null,
    string? DateFrom = null,
    string? DateTo = null,
    int Page = 1,
    int PageSize = 20) : IRequest<SearchCasesResult>;

public record CaseSummary(
    int Id,
    string CaseNumber,
    string Title,
    string Status,
    string PracticeGroup,
    string ClientName,
    string OpenDate,
    decimal EstimatedValue,
    string? LeadAttorney);

public record SearchCasesResult(
    IReadOnlyList<CaseSummary> Results,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);
