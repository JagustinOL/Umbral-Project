using MediatR;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Entities;

namespace ScoringAudit.Application.Events;

public sealed record ProcessSessionFinalizedCommand(SessionFinalizedIntegrationEvent Event) : IRequest;

public sealed class ProcessSessionFinalizedHandler : IRequestHandler<ProcessSessionFinalizedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ProcessSessionFinalizedHandler(
        ITeamLedgerRepository ledgerRepository,
        IAuditLogRepository auditLogRepository)
    {
        _ledgerRepository = ledgerRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task Handle(ProcessSessionFinalizedCommand request, CancellationToken cancellationToken)
    {
        var ledgers = await _ledgerRepository.GetBySessionAsync(request.Event.SessionId, cancellationToken);
        foreach (var ledger in ledgers.Where(l => !l.IsClosed))
        {
            ledger.Close();
            await _ledgerRepository.SaveAsync(ledger, cancellationToken);
        }

        var auditLog = await _auditLogRepository.GetBySessionAsync(request.Event.SessionId, cancellationToken);
        if (auditLog is null || auditLog.IsClosed)
            return;

        var status = string.Equals(request.Event.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            ? "Cancelled"
            : "Finished";
        auditLog.RecordEvent(
            status == "Cancelled" ? SessionEventType.SessionCancelled : SessionEventType.SessionFinalized,
            request.Event.EventId,
            status == "Cancelled" ? "La sesión fue cancelada." : "La sesión fue finalizada.");
        auditLog.Close(status, request.Event.FinalizedAtUtc);
        await _auditLogRepository.SaveAsync(auditLog, cancellationToken);
    }
}
