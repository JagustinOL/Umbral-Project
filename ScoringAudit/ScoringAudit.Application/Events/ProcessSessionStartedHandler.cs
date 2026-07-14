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

    public ProcessSessionStartedHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(ProcessSessionStartedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        if (await _auditLogRepository.GetBySessionAsync(evt.SessionId, cancellationToken) is not null)
            return;

        var auditLog = AuditLog.Create(evt.SessionId, evt.MissionRef, evt.OperatorRef, evt.StartedAtUtc);
        auditLog.RecordEvent(SessionEventType.SessionStarted, evt.EventId, "La sesión fue iniciada.");
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
