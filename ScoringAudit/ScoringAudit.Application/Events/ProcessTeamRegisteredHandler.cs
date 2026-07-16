using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;

namespace ScoringAudit.Application.Events;

public sealed record ProcessTeamRegisteredCommand(TeamRegisteredIntegrationEvent Event) : IRequest;

public sealed class ProcessTeamRegisteredHandler : IRequestHandler<ProcessTeamRegisteredCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProcessTeamRegisteredHandler(
        ITeamLedgerRepository ledgerRepository,
        IAuditLogRepository auditLogRepository)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(ProcessTeamRegisteredCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var teamName = string.IsNullOrWhiteSpace(evt.TeamName)
            ? $"Team-{evt.TeamId:N}"[..12]
            : evt.TeamName;

        var existing = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken);
        if (existing is null)
        {
            var ledger = TeamLedger.Create(evt.TeamId, evt.SessionId, teamName);
            await _ledgerRepository.SaveAsync(ledger, cancellationToken);
        }

        // Si el AuditLog ya existe (registro tardío), agregar el evento ahora.
        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken);
        if (auditLog is null || auditLog.IsClosed)
            return;

        if (auditLog.Events.Any(e =>
                e.EventType == SessionEventType.TeamRegistered && e.TeamRef == evt.TeamId))
            return;

        var (description, metadata) = AuditEventDisplay.TeamRegistered(teamName);
        auditLog.RecordEvent(
            SessionEventType.TeamRegistered,
            evt.EventId,
            description,
            evt.TeamId,
            metadata: metadata);
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
