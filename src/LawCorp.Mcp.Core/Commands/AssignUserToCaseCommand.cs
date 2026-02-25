using MediatR;

namespace LawCorp.Mcp.Core.Commands;

public record AssignUserToCaseCommand(
    int CaseId,
    int UserId,
    string Role) : IRequest<CommandResult>;
