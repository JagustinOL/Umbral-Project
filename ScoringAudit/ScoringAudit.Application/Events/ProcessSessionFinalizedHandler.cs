using MediatR;
using ScoringAudit.Domain.Repositories;
using Umbral.Shared.Messaging.IntegrationEvents;

namespace ScoringAudit.Application.Events;

public sealed record ProcessSessionFinalizedCommand(SessionFinalizedIntegrationEvent Event) : IRequest;

public sealed class ProcessSessionFinalizedHandler : IRequestHandler<ProcessSessionFinalizedCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;

    public ProcessSessionFinalizedHandler(ITeamLedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task Handle(ProcessSessionFinalizedCommand request, CancellationToken cancellationToken)
    {
        var ledgers = await _ledgerRepository.GetBySessionAsync(request.Event.SessionId, cancellationToken);
        foreach (var ledger in ledgers.Where(l => !l.IsClosed))
        {
            ledger.Close();
            await _ledgerRepository.SaveAsync(ledger, cancellationToken);
        }
    }
}
