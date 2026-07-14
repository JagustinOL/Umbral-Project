using System.Text.Json;
using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Application.Events;

public sealed record ProcessHintReleasedCommand(HintReleasedIntegrationEvent Event) : IRequest;

public sealed class ProcessHintReleasedHandler : IRequestHandler<ProcessHintReleasedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProcessHintReleasedHandler(ITeamLedgerRepository ledgerRepository, IAuditLogRepository auditLogRepository)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(ProcessHintReleasedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe TeamLedger para equipo {evt.TeamId} en sesión {evt.SessionId}.");

        ledger.ApplyPenalty(
            evt.PenaltyPoints,
            PenaltyReason.ForHintUsage(evt.HintId, 0),
            ScoreEntryType.HintPenalty,
            evt.EventId);
        await _ledgerRepository.SaveAsync(ledger, cancellationToken);

        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe AuditLog para sesión {evt.SessionId}.");
        auditLog.RecordEvent(
            SessionEventType.HintReleased,
            evt.EventId,
            "Se liberó una pista y se aplicó su penalización.",
            evt.TeamId,
            evt.MissionNodeId,
            JsonSerializer.Serialize(new { evt.HintId, evt.PenaltyPoints }));
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
