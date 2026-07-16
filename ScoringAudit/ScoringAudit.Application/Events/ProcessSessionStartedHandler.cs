using MediatR;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;

namespace ScoringAudit.Application.Events;

public sealed record ProcessSessionStartedCommand(SessionStartedIntegrationEvent Event) : IRequest;

public sealed class ProcessSessionStartedHandler : IRequestHandler<ProcessSessionStartedCommand>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITeamLedgerRepository _ledgerRepository;

    public ProcessSessionStartedHandler(
        IAuditLogRepository auditLogRepository,
        ITeamLedgerRepository ledgerRepository)
    {
        _auditLogRepository = auditLogRepository;
        _ledgerRepository = ledgerRepository;
    }

    public async Task Handle(ProcessSessionStartedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var participatingTeams = evt.ParticipatingTeamIds ?? [];
        var auditLog = await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken);
        if (auditLog is null)
        {
            auditLog = AuditLog.Create(evt.SessionId, evt.MissionRef, evt.OperatorRef, evt.StartedAtUtc);
            var (description, metadata) = AuditEventDisplay.SessionStarted(participatingTeams.Count, evt.StartedAtUtc);
            auditLog.RecordEvent(
                SessionEventType.SessionStarted,
                evt.EventId,
                description,
                metadata: metadata);
            await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
        }

        foreach (var teamId in participatingTeams)
        {
            var existing = await _ledgerRepository.GetByTeamAndSessionAsync(
                teamId, evt.SessionId, cancellationToken);
            if (existing is null)
            {
                existing = TeamLedger.Create(teamId, evt.SessionId, $"Team-{teamId:N}"[..12]);
                await _ledgerRepository.SaveAsync(existing, cancellationToken);
            }

            // Los TeamRegistered suelen llegar antes del AuditLog; se materializan aquí.
            if (!auditLog.Events.Any(e =>
                    e.EventType == SessionEventType.TeamRegistered && e.TeamRef == teamId))
            {
                var (description, metadata) = AuditEventDisplay.TeamRegistered(existing.TeamName);
                auditLog.RecordEvent(
                    SessionEventType.TeamRegistered,
                    Guid.NewGuid(),
                    description,
                    teamId,
                    metadata: metadata);
            }
        }

        if (participatingTeams.Count > 0)
            await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
