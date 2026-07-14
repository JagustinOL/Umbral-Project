using MediatR;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;

namespace ScoringAudit.Application.Queries;

public sealed record GetSessionAuditDetailQuery(Guid SessionId, bool IsAdmin, Guid? OperatorId)
    : IRequest<SessionAuditDetailDto?>;

public sealed record SessionAuditEventDto(
    Guid EventId,
    SessionEventType EventType,
    DateTime OccurredAtUtc,
    string Description,
    Guid? TeamId,
    Guid? MissionNodeId,
    string? Metadata);

public sealed record SessionAuditPenaltyDto(
    Guid TeamId,
    int Points,
    string Category,
    string Description,
    Guid? AppliedByOperatorId,
    DateTime RecordedAtUtc);

public sealed record SessionAuditEvidenceDto(
    Guid TeamId,
    Guid MissionNodeId,
    int Points,
    double ElapsedSeconds,
    DateTime RecordedAtUtc);

public sealed record SessionAuditDetailDto(
    Guid SessionId,
    Guid MissionId,
    Guid OperatorId,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status,
    IReadOnlyList<SessionAuditEventDto> Timeline,
    IReadOnlyList<SessionAuditPenaltyDto> Penalties,
    IReadOnlyList<SessionAuditEvidenceDto> Evidences,
    IReadOnlyList<RankingItemDto> Ranking);

public sealed class GetSessionAuditDetailHandler
    : IRequestHandler<GetSessionAuditDetailQuery, SessionAuditDetailDto?>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly RankingManagerService _rankingManager;

    public GetSessionAuditDetailHandler(
        IAuditLogRepository auditLogRepository,
        ITeamLedgerRepository ledgerRepository,
        RankingManagerService rankingManager)
    {
        _auditLogRepository = auditLogRepository;
        _ledgerRepository = ledgerRepository;
        _rankingManager = rankingManager;
    }

    public async Task<SessionAuditDetailDto?> Handle(
        GetSessionAuditDetailQuery request,
        CancellationToken cancellationToken)
    {
        var auditLog = await _auditLogRepository.GetBySessionAsync(request.SessionId, cancellationToken);
        if (auditLog is null || !auditLog.IsClosed ||
            (!request.IsAdmin && auditLog.OperatorRef != request.OperatorId))
            return null;

        var ledgers = await _ledgerRepository.GetBySessionAsync(request.SessionId, cancellationToken);
        var ranking = _rankingManager.BuildRanking(ledgers)
            .Select(x => new RankingItemDto(
                x.TeamId, x.TeamName, x.TotalScore, x.CompletedNodes, x.TotalElapsedSeconds))
            .ToList();

        return new SessionAuditDetailDto(
            auditLog.SessionRef,
            auditLog.MissionRef,
            auditLog.OperatorRef,
            auditLog.StartedAtUtc,
            auditLog.EndedAtUtc,
            auditLog.Status,
            auditLog.Events.OrderBy(x => x.OccurredAtUtc).Select(x => new SessionAuditEventDto(
                x.SourceEventId, x.EventType, x.OccurredAtUtc, x.Description, x.TeamRef, x.MissionNodeRef, x.Metadata)).ToList(),
            ledgers.SelectMany(x => x.Entries
                .Where(y => y.PenaltyReason is not null)
                .Select(y => new SessionAuditPenaltyDto(
                    x.TeamRef, y.Points, y.PenaltyReason!.Category.ToString(),
                    y.PenaltyReason.Description, y.PenaltyReason.AppliedByOperatorId, y.RecordedAtUtc))).ToList(),
            ledgers.SelectMany(x => x.Entries
                .Where(y => y.Origin is not null)
                .Select(y => new SessionAuditEvidenceDto(
                    x.TeamRef, y.Origin!.MissionNodeId, y.Points, y.Origin.ElapsedSeconds, y.RecordedAtUtc))).ToList(),
            ranking);
    }
}
