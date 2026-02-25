using MediatR;

namespace LawCorp.Mcp.Core.Queries;

public record ListDocumentsByCaseQuery(
    int CaseId,
    string? DocumentType = null,
    string? Status = null) : IRequest<SearchDocumentsResult>;
