using ScoringAudit.Application.Queries;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.ReadModels;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.ValueObjects;
using Xunit;

namespace ScoringAudit.Application.Tests.Queries;

public sealed class GetTeamPenaltiesHandlerTests
{
    [Fact]
    public async Task Handle_WhenLedgerMissing_ReturnsEmpty()
    {
        var handler = new GetTeamPenaltiesHandler(new InMemoryTeamLedgerRepository());
        var result = await handler.Handle(
            new GetTeamPenaltiesQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyNegativeEntriesNewestFirst()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var ledger = TeamLedger.Create(teamId, sessionId, "Alpha");
        ledger.AddEvidenceScore(
            ScoreOrigin.FromStrategyResult(Guid.NewGuid(), "Trivia", 100, 1m, 10, 100),
            Guid.NewGuid());
        ledger.ApplyPenalty(10, PenaltyReason.ForManualPenalty("Conducta antideportiva"), ScoreEntryType.ManualPenalty, Guid.NewGuid());
        ledger.ApplyPenalty(5, PenaltyReason.ForHintUsage(Guid.NewGuid(), 1), ScoreEntryType.HintPenalty, Guid.NewGuid());

        var repo = new InMemoryTeamLedgerRepository();
        await repo.SaveAsync(ledger);

        var handler = new GetTeamPenaltiesHandler(repo);
        var result = await handler.Handle(new GetTeamPenaltiesQuery(sessionId, teamId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.PenaltyPoints > 0));
        Assert.Equal(5, result[0].PenaltyPoints); // newest first
        Assert.Equal(10, result[1].PenaltyPoints);
        Assert.Equal("Conducta antideportiva", result[1].Reason);
        Assert.Equal(nameof(PenaltyCategory.ManualOperator), result[1].Category);
    }

    private sealed class InMemoryTeamLedgerRepository : ITeamLedgerRepository
    {
        private readonly Dictionary<(Guid Team, Guid Session), TeamLedger> _items = [];

        public Task<TeamLedger?> GetByTeamAndSessionAsync(
            Guid teamRef, Guid sessionRef, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.GetValueOrDefault((teamRef, sessionRef)));

        public Task<IReadOnlyList<TeamLedger>> GetBySessionAsync(
            Guid sessionRef, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TeamLedger>>(
                _items.Values.Where(x => x.SessionRef == sessionRef).ToList());

        public Task<IReadOnlyList<TopScoreAcrossSessions>> GetTopScoresAcrossFinishedSessionsAsync(
            Guid? operatorRef, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TopScoreAcrossSessions>>([]);

        public Task SaveAsync(TeamLedger ledger, CancellationToken cancellationToken = default)
        {
            _items[(ledger.TeamRef, ledger.SessionRef)] = ledger;
            return Task.CompletedTask;
        }
    }
}
