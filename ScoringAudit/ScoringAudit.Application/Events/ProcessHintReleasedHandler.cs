using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Application.Events;

public sealed record ProcessHintReleasedCommand(HintReleasedIntegrationEvent Event) : IRequest;

public sealed class ProcessHintReleasedHandler : IRequestHandler<ProcessHintReleasedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly RankingManagerService _rankingManager;
    private readonly ITeamScoreUpdatePublisher _scoreUpdatePublisher;

    public ProcessHintReleasedHandler(
        ITeamLedgerRepository ledgerRepository,
        IAuditLogRepository auditLogRepository,
        RankingManagerService rankingManager,
        ITeamScoreUpdatePublisher scoreUpdatePublisher)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
        _rankingManager = rankingManager;
        _scoreUpdatePublisher = scoreUpdatePublisher;
    }

    public async Task Handle(ProcessHintReleasedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe TeamLedger para equipo {evt.TeamId} en sesión {evt.SessionId}.");

        var penaltyApplied = evt.PenaltyPoints > 0;
        if (penaltyApplied)
        {
            ledger.ApplyPenalty(
                evt.PenaltyPoints,
                PenaltyReason.ForHintUsage(evt.HintId, 0),
                ScoreEntryType.HintPenalty,
                evt.EventId);
            await _ledgerRepository.SaveAsync(ledger, cancellationToken);
        }

        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe AuditLog para sesión {evt.SessionId}.");
        var (description, metadata) = AuditEventDisplay.HintReleased(
            ledger.TeamName,
            evt.NodeType,
            evt.NodeTitle,
            evt.MissionNodeId,
            evt.HintId,
            evt.HintOrder,
            evt.PenaltyPoints,
            evt.WasManualRelease);
        auditLog.RecordEvent(
            SessionEventType.HintReleased,
            evt.EventId,
            description,
            evt.TeamId,
            evt.MissionNodeId,
            metadata);
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);

        if (penaltyApplied)
        {
            var ledgers = await _ledgerRepository.GetBySessionAsync(evt.SessionId, cancellationToken);
            await _scoreUpdatePublisher.PublishAsync(
                new TeamScoreUpdatedIntegrationEvent
                {
                    SessionId = evt.SessionId,
                    TeamId = evt.TeamId,
                    NewTotalScore = ledger.TotalScore,
                    Ranking = RankingSnapshotMapper.ToDto(_rankingManager.BuildRanking(ledgers))
                },
                cancellationToken);
        }
    }
}
