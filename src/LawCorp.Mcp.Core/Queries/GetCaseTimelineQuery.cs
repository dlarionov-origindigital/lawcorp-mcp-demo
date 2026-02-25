using MediatR;

namespace LawCorp.Mcp.Core.Queries;

public record GetCaseTimelineQuery(
    int CaseId,
    string? EventType = null) : IRequest<GetCaseTimelineResult>;

public record TimelineEvent(
    int Id,
    string Type,
    string Title,
    string Description,
    string Date,
    int CreatedById);

public record GetCaseTimelineResult(
    int CaseId,
    int EventCount,
    IReadOnlyList<TimelineEvent> Events,
    string? Error = null);
