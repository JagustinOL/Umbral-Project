using System.Text.Json;
using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.ValueObjects;

namespace ScoringAudit.Application.Events;

public sealed record ProcessManualPenaltyCommand(ManualPenaltyAppliedIntegrationEvent Event) : IRequest;

public sealed class ProcessManualPenaltyHandler : IRequestHandler<ProcessManualPenaltyCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProcessManualPenaltyHandler(ITeamLedgerRepository ledgerRepository, IAuditLogRepository auditLogRepository)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(ProcessManualPenaltyCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe TeamLedger para equipo {evt.TeamId} en sesión {evt.SessionId}.");

        ledger.ApplyPenalty(
            evt.PenaltyPoints,
            PenaltyReason.ForManualPenalty(evt.Reason, evt.OperatorRef),
            ScoreEntryType.ManualPenalty,
            evt.EventId);
        await _ledgerRepository.SaveAsync(ledger, cancellationToken);

        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe AuditLog para sesión {evt.SessionId}.");
        auditLog.RecordEvent(
            SessionEventType.ManualPenaltyApplied,
            evt.EventId,
            "El operador aplicó una penalización manual.",
            evt.TeamId,
            metadata: JsonSerializer.Serialize(new { evt.OperatorRef, evt.PenaltyPoints, evt.Reason }));
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
