using MediatR;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Domain.ValueObjects;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;

namespace ScoringAudit.Application.Events;

public sealed record ProcessEvidenceValidatedCommand(EvidenceValidatedIntegrationEvent Event) : IRequest;

public sealed class ProcessEvidenceValidatedHandler : IRequestHandler<ProcessEvidenceValidatedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ScoreCalculatorService _scoreCalculator;
    private readonly RankingManagerService _rankingManager;
    private readonly ITeamScoreUpdatePublisher _scoreUpdatePublisher;

    public ProcessEvidenceValidatedHandler(
        ITeamLedgerRepository ledgerRepository,
        IAuditLogRepository auditLogRepository,
        ScoreCalculatorService scoreCalculator,
        RankingManagerService rankingManager,
        ITeamScoreUpdatePublisher scoreUpdatePublisher)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
        _scoreCalculator = scoreCalculator;
        _rankingManager = rankingManager;
        _scoreUpdatePublisher = scoreUpdatePublisher;
    }

    public async Task Handle(ProcessEvidenceValidatedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken);
        if (ledger is null)
        {
            // Tolerancia: TeamRegistered pudo haberse perdido si ScoringAudit estaba caído.
            ledger = TeamLedger.Create(
                evt.TeamId,
                evt.SessionId,
                $"Team-{evt.TeamId:N}"[..12]);
        }

        var finalScore = _scoreCalculator.Calculate(
            evt.NodeType,
            evt.BaseScore,
            evt.DifficultyMultiplier,
            evt.ElapsedSeconds);

        var origin = ScoreOrigin.FromStrategyResult(
            evt.MissionNodeId,
            evt.NodeType,
            evt.BaseScore,
            evt.DifficultyMultiplier,
            evt.ElapsedSeconds,
            finalScore);

        var alreadyRewarded = ledger.Entries.Any(e =>
            e.EntryType == ScoreEntryType.EvidenceRewarded
            && e.Origin?.MissionNodeId == evt.MissionNodeId);

        if (!alreadyRewarded)
        {
            ledger.AddEvidenceScore(origin, evt.EventId);
            await _ledgerRepository.SaveAsync(ledger, cancellationToken);

            var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken);
            if (auditLog is not null)
            {
                var (description, metadata) = AuditEventDisplay.EvidenceValidated(
                    ledger.TeamName,
                    evt.NodeType,
                    evt.NodeTitle,
                    evt.MissionNodeId,
                    finalScore,
                    evt.ElapsedSeconds);
                auditLog.RecordEvent(
                    SessionEventType.EvidenceValidated,
                    evt.EventId,
                    description,
                    evt.TeamId,
                    evt.MissionNodeId,
                    metadata);
                await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
            }
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
