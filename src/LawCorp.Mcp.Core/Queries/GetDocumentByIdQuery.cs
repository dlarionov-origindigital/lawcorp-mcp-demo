using MediatR;

namespace LawCorp.Mcp.Core.Queries;

public record GetDocumentByIdQuery(int DocumentId) : IRequest<GetDocumentResult>;

public record DocumentDetail(
    int Id,
    string Title,
    string DocumentType,
    string Status,
    string Content,
    string AuthorName,
    int AuthorId,
    int CaseId,
    string CaseNumber,
    bool IsPrivileged,
    bool IsRedacted,
    string CreatedDate,
    string ModifiedDate);

public record GetDocumentResult(DocumentDetail? Document, string? Error = null);
