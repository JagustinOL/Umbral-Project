using MediatR;
using ScoringAudit.Domain.Repositories;

namespace ScoringAudit.Application.Queries;

public sealed record GetHistoricalSessionsQuery(
    DateTime? StartDate,
    DateTime? EndDate,
    int Page,
    int PageSize,
    bool IsAdmin,
    Guid? OperatorId) : IRequest<HistoricalSessionsPageDto>;

public sealed record HistoricalSessionDto(
    Guid SessionId,
    Guid MissionId,
    Guid OperatorId,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);

public sealed record HistoricalSessionsPageDto(
    IReadOnlyList<HistoricalSessionDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class GetHistoricalSessionsHandler
    : IRequestHandler<GetHistoricalSessionsQuery, HistoricalSessionsPageDto>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetHistoricalSessionsHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<HistoricalSessionsPageDto> Handle(
        GetHistoricalSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var operatorRef = request.IsAdmin ? null : request.OperatorId;
        if (!request.IsAdmin && operatorRef is null)
            return new HistoricalSessionsPageDto([], 0, page, pageSize);

        var result = await _auditLogRepository.GetClosedPaginatedAsync(
            request.StartDate,
            request.EndDate,
            operatorRef,
            page,
            pageSize,
            cancellationToken);

        return new HistoricalSessionsPageDto(
            result.Items.Select(x => new HistoricalSessionDto(
                x.SessionRef, x.MissionRef, x.OperatorRef, x.StartedAtUtc, x.EndedAtUtc, x.Status)).ToList(),
            result.TotalCount,
            page,
            pageSize);
    }
}
