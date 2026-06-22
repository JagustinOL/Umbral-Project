using MediatR;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Application.Messaging;

namespace ScoringAudit.Application.Events;

public sealed record ProcessTeamRegisteredCommand(TeamRegisteredIntegrationEvent Event) : IRequest;

public sealed class ProcessTeamRegisteredHandler : IRequestHandler<ProcessTeamRegisteredCommand>
{
    private readonly ITeamLedgerRepository _ledgerRepository;

    public ProcessTeamRegisteredHandler(ITeamLedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task Handle(ProcessTeamRegisteredCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var existing = await _ledgerRepository.GetByTeamAndSessionAsync(evt.TeamId, evt.SessionId, cancellationToken);
        if (existing is not null)
            return;

        var ledger = TeamLedger.Create(evt.TeamId, evt.SessionId, evt.TeamName);
        await _ledgerRepository.SaveAsync(ledger, cancellationToken);
    }
}
