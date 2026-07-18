using MediatR;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;

namespace ScoringAudit.Application.Queries;

public sealed record GetTeamPenaltiesQuery(Guid SessionId, Guid TeamId)
    : IRequest<IReadOnlyList<TeamPenaltyDto>>;

public sealed record TeamPenaltyDto(
    Guid EntryId,
    int PenaltyPoints,
    string Reason,
    string Category,
    DateTime AppliedAtUtc);

public sealed class GetTeamPenaltiesHandler
    : IRequestHandler<GetTeamPenaltiesQuery, IReadOnlyList<TeamPenaltyDto>>
{
    private readonly ITeamLedgerRepository _ledgerRepository;

    public GetTeamPenaltiesHandler(ITeamLedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task<IReadOnlyList<TeamPenaltyDto>> Handle(
        GetTeamPenaltiesQuery request,
        CancellationToken cancellationToken)
    {
        var ledger = await _ledgerRepository.GetByTeamAndSessionAsync(
            request.TeamId, request.SessionId, cancellationToken);

        if (ledger is null)
            return [];

        return ledger.Entries
            .Where(e => e.Points < 0)
            .OrderByDescending(e => e.RecordedAtUtc)
            .Select(e => new TeamPenaltyDto(
                EntryId: e.Id,
                PenaltyPoints: Math.Abs(e.Points),
                Reason: e.PenaltyReason?.Description
                    ?? (e.EntryType == ScoreEntryType.HintPenalty
                        ? "Penalización por uso de pista."
                        : "Penalización aplicada."),
                Category: e.PenaltyReason?.Category.ToString()
                    ?? e.EntryType.ToString(),
                AppliedAtUtc: e.RecordedAtUtc))
            .ToList();
    }
}
