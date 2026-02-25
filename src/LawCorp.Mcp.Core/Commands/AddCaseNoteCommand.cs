using MediatR;

namespace LawCorp.Mcp.Core.Commands;

public record AddCaseNoteCommand(
    int CaseId,
    string Content,
    bool IsPrivileged = false) : IRequest<CommandResult>;
