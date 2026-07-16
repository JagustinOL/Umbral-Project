using ScoringAudit.Application.Events;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using ScoringAudit.Domain.Services;
using Xunit;

namespace ScoringAudit.Application.Tests.Events;

public sealed class ProcessEvidenceValidatedHandlerTests
{
    [Fact]
    public async Task Handle_WhenBaseScorePositive_CreditsLedgerPublishesRankingWithPosition()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var ledgerRepo = new InMemoryTeamLedgerRepository();
        var auditRepo = new InMemoryAuditLogRepository();
        var publisher = new CapturingScoreUpdatePublisher();

        var ledger = TeamLedger.Create(teamId, sessionId, "Alpha");
        await ledgerRepo.SaveAsync(ledger);

        var auditLog = AuditLog.Create(sessionId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        await auditRepo.SaveAsync(auditLog);

        var calculator = new ScoreCalculatorService(
        [
            new TriviaScoreStrategy(),
            new TreasureHuntScoreStrategy()
        ]);

        var handler = new ProcessEvidenceValidatedHandler(
            ledgerRepo,
            auditRepo,
            calculator,
            new RankingManagerService(),
            publisher);

        var evt = new EvidenceValidatedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            SessionId = sessionId,
            EvidenceSubmissionId = Guid.NewGuid(),
            TeamId = teamId,
            MissionNodeId = nodeId,
            NodeType = "Trivia",
            BaseScore = 100,
            DifficultyMultiplier = 1.0m,
            ElapsedSeconds = 45
        };

        await handler.Handle(new ProcessEvidenceValidatedCommand(evt), CancellationToken.None);

        var updated = await ledgerRepo.GetByTeamAndSessionAsync(teamId, sessionId);
        Assert.NotNull(updated);
        Assert.Equal(100, updated!.TotalScore);

        Assert.NotNull(publisher.Last);
        Assert.Equal(teamId, publisher.Last!.TeamId);
        Assert.Equal(updated.TotalScore, publisher.Last.NewTotalScore);
        var first = Assert.Single(publisher.Last.Ranking);
        Assert.Equal(1, first.Position);
        Assert.Equal(updated.TotalScore, first.TotalScore);
    }

    [Fact]
    public async Task Handle_WhenLedgerMissing_CreatesLedgerAndCreditsScore()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var ledgerRepo = new InMemoryTeamLedgerRepository();
        var auditRepo = new InMemoryAuditLogRepository();
        var publisher = new CapturingScoreUpdatePublisher();

        var handler = new ProcessEvidenceValidatedHandler(
            ledgerRepo,
            auditRepo,
            new ScoreCalculatorService([new TriviaScoreStrategy(), new TreasureHuntScoreStrategy()]),
            new RankingManagerService(),
            publisher);

        var evt = new EvidenceValidatedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            SessionId = sessionId,
            EvidenceSubmissionId = Guid.NewGuid(),
            TeamId = teamId,
            MissionNodeId = Guid.NewGuid(),
            NodeType = "TreasureHunt",
            BaseScore = 100,
            DifficultyMultiplier = 1.0m,
            ElapsedSeconds = 80
        };

        await handler.Handle(new ProcessEvidenceValidatedCommand(evt), CancellationToken.None);

        var ledger = await ledgerRepo.GetByTeamAndSessionAsync(teamId, sessionId);
        Assert.NotNull(ledger);
        Assert.Equal(100, ledger!.TotalScore);
        Assert.NotNull(publisher.Last);
        Assert.Equal(100, Assert.Single(publisher.Last!.Ranking).TotalScore);
    }

    [Fact]
    public async Task Handle_WhenBaseScoreIsZero_ThrowsAndDoesNotCredit()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        var ledgerRepo = new InMemoryTeamLedgerRepository();
        var auditRepo = new InMemoryAuditLogRepository();
        var publisher = new CapturingScoreUpdatePublisher();

        await ledgerRepo.SaveAsync(TeamLedger.Create(teamId, sessionId, "Beta"));
        await auditRepo.SaveAsync(AuditLog.Create(sessionId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));

        var handler = new ProcessEvidenceValidatedHandler(
            ledgerRepo,
            auditRepo,
            new ScoreCalculatorService([new TriviaScoreStrategy(), new TreasureHuntScoreStrategy()]),
            new RankingManagerService(),
            publisher);

        var evt = new EvidenceValidatedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            SessionId = sessionId,
            EvidenceSubmissionId = Guid.NewGuid(),
            TeamId = teamId,
            MissionNodeId = Guid.NewGuid(),
            NodeType = "Trivia",
            BaseScore = 0,
            DifficultyMultiplier = 1.0m,
            ElapsedSeconds = 10
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(new ProcessEvidenceValidatedCommand(evt), CancellationToken.None));

        Assert.Null(publisher.Last);
        var ledger = await ledgerRepo.GetByTeamAndSessionAsync(teamId, sessionId);
        Assert.Equal(0, ledger!.TotalScore);
    }

    private sealed class CapturingScoreUpdatePublisher : ITeamScoreUpdatePublisher
    {
        public TeamScoreUpdatedIntegrationEvent? Last { get; private set; }

        public Task PublishAsync(TeamScoreUpdatedIntegrationEvent message, CancellationToken cancellationToken = default)
        {
            Last = message;
            return Task.CompletedTask;
        }
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

        public Task SaveAsync(TeamLedger ledger, CancellationToken cancellationToken = default)
        {
            _items[(ledger.TeamRef, ledger.SessionRef)] = ledger;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditLogRepository : IAuditLogRepository
    {
        private readonly Dictionary<Guid, AuditLog> _logs = [];

        public Task<AuditLog?> GetBySessionAsync(Guid sessionRef, CancellationToken cancellationToken = default) =>
            Task.FromResult(_logs.GetValueOrDefault(sessionRef));

        public Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetClosedPaginatedAsync(
            DateTime? startDate, DateTime? endDate, Guid? operatorRef, int page, int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<AuditLog>, int)>(([], 0));

        public Task SaveAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            _logs[auditLog.SessionRef] = auditLog;
            return Task.CompletedTask;
        }
    }
}
