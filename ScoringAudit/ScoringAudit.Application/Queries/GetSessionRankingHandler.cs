using MediatR;
using ScoringAudit.Application.Queries;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;

namespace ScoringAudit.Application.Queries;

public sealed record GetSessionRankingQuery(Guid SessionId) : IRequest<IReadOnlyList<RankingItemDto>>;

public sealed record RankingItemDto(
    int Position,
    Guid TeamId,
    string TeamName,
    int TotalScore,
    int CompletedNodes,
    double LastElapsedSeconds);

public sealed class GetSessionRankingHandler : IRequestHandler<GetSessionRankingQuery, IReadOnlyList<RankingItemDto>>
{
    private readonly RankingManagerService _rankingManager;
    private readonly ITeamLedgerRepository _ledgerRepository;

    public GetSessionRankingHandler(
        RankingManagerService rankingManager,
        ITeamLedgerRepository ledgerRepository)
    {
        _rankingManager = rankingManager;
        _ledgerRepository = ledgerRepository;
    }

    public async Task<IReadOnlyList<RankingItemDto>> Handle(
        GetSessionRankingQuery request,
        CancellationToken cancellationToken)
    {
        var ledgers = await _ledgerRepository.GetBySessionAsync(request.SessionId, cancellationToken);
        var ranking = _rankingManager.BuildRanking(ledgers);

        return ranking
            .Select(r => new RankingItemDto(
                r.Position,
                r.TeamId,
                r.TeamName,
                r.TotalScore,
                r.CompletedNodes,
                r.TotalElapsedSeconds))
            .ToList();
    }
}
