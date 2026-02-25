using MediatR;

namespace LawCorp.Mcp.Core.Commands;

public record UpdateCaseStatusCommand(
    int CaseId,
    string NewStatus,
    string? Reason = null) : IRequest<CommandResult>;

public record CommandResult(
    bool Success,
    string Message,
    object? Data = null,
    string? Error = null);
