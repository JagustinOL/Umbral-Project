using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Application.Events;

public sealed record ProcessManualPenaltyCommand(ManualPenaltyAppliedIntegrationEvent Event) : IRequest;

public sealed class ProcessManualPenaltyHandler : IRequestHandler<ProcessManualPenaltyCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly RankingManagerService _rankingManager;
    private readonly ITeamScoreUpdatePublisher _scoreUpdatePublisher;

    public ProcessManualPenaltyHandler(
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

    public async Task Handle(ProcessManualPenaltyCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken);
        if (ledger is null)
        {
            ledger = TeamLedger.Create(
                evt.TeamId,
                evt.SessionId,
                $"Team-{evt.TeamId:N}"[..12]);
        }

        ledger.ApplyPenalty(
            evt.PenaltyPoints,
            PenaltyReason.ForManualPenalty(evt.Reason, evt.OperatorRef),
            ScoreEntryType.ManualPenalty,
            evt.EventId);
        await _ledgerRepository.SaveAsync(ledger, cancellationToken);

        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken);
        if (auditLog is not null)
        {
            var (description, metadata) = AuditEventDisplay.ManualPenalty(
                ledger.TeamName,
                evt.PenaltyPoints,
                evt.Reason,
                evt.OperatorRef);
            auditLog.RecordEvent(
                SessionEventType.ManualPenaltyApplied,
                evt.EventId,
                description,
                evt.TeamId,
                metadata: metadata);
            await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
        }

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
