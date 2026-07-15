using MediatR;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Domain.ValueObjects;
using ScoringAudit.Application.Messaging;
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
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No existe TeamLedger para equipo {evt.TeamId} en sesión {evt.SessionId}.");

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

        ledger.AddEvidenceScore(origin, evt.EventId);
        await _ledgerRepository.SaveAsync(ledger, cancellationToken);

        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe AuditLog para sesión {evt.SessionId}.");
        auditLog.RecordEvent(
            SessionEventType.EvidenceValidated,
            evt.EventId,
            "Evidencia validada y puntaje acreditado.",
            evt.TeamId,
            evt.MissionNodeId,
            $"{{\"score\":{finalScore},\"elapsedSeconds\":{evt.ElapsedSeconds}}}");
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);

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
