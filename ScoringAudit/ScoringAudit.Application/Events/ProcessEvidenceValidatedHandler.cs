using MediatR;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using ScoringAudit.Domain.ValueObjects;
using ScoringAudit.Application.Messaging;

namespace ScoringAudit.Application.Events;

public sealed record ProcessEvidenceValidatedCommand(EvidenceValidatedIntegrationEvent Event) : IRequest;

public sealed class ProcessEvidenceValidatedHandler : IRequestHandler<ProcessEvidenceValidatedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly ScoreCalculatorService _scoreCalculator;

    public ProcessEvidenceValidatedHandler(
        ITeamLedgerRepository ledgerRepository,
        ScoreCalculatorService scoreCalculator)
    {
        _ledgerRepository = ledgerRepository;
        _scoreCalculator = scoreCalculator;
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
    }
}
