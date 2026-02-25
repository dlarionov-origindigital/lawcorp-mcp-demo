using MediatR;

namespace LawCorp.Mcp.Core.Queries;

public record SearchDocumentsQuery(
    string Query,
    string? DocumentType = null,
    int? CaseId = null,
    int? AuthorId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<SearchDocumentsResult>;

public record DocumentSummary(
    int Id,
    string Title,
    string DocumentType,
    string Status,
    string AuthorName,
    int CaseId,
    string CaseNumber,
    bool IsPrivileged,
    string CreatedDate,
    string ModifiedDate);

public record SearchDocumentsResult(
    IReadOnlyList<DocumentSummary> Results,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);
