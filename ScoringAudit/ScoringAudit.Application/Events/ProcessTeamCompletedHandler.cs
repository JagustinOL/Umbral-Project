using System.Text.Json;
using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;

namespace ScoringAudit.Application.Events;

public sealed record ProcessTeamCompletedCommand(TeamCompletedMissionIntegrationEvent Event) : IRequest;

public sealed class ProcessTeamCompletedHandler : IRequestHandler<ProcessTeamCompletedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProcessTeamCompletedHandler(
        ITeamLedgerRepository ledgerRepository,
        IAuditLogRepository auditLogRepository)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(ProcessTeamCompletedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe TeamLedger para equipo {evt.TeamId} en sesión {evt.SessionId}.");
        ledger.SetCompletionElapsedSeconds(evt.ElapsedSeconds);
        await _ledgerRepository.SaveAsync(ledger, cancellationToken);

        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe AuditLog para sesión {evt.SessionId}.");
        auditLog.RecordEvent(
            SessionEventType.TeamCompletedMission,
            evt.EventId,
            "El equipo completó la misión.",
            evt.TeamId,
            metadata: JsonSerializer.Serialize(new { evt.ElapsedSeconds, evt.CompletedAtUtc }));
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
