using ScoringAudit.Application.Events;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.ReadModels;
using ScoringAudit.Domain.Repositories;
using Xunit;

namespace ScoringAudit.Application.Tests.Events;

public sealed class ProcessSessionStartedHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAuditLogAndRecordsSessionStarted()
    {
        var auditRepo = new InMemoryAuditLogRepository();
        var ledgerRepo = new InMemoryTeamLedgerRepository();
        var handler = new ProcessSessionStartedHandler(auditRepo, ledgerRepo);
        var teamId = Guid.NewGuid();
        var evt = new SessionStartedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            MissionRef = Guid.NewGuid(),
            OperatorRef = Guid.NewGuid(),
            ParticipatingTeamIds = [teamId],
            StartedAtUtc = DateTime.UtcNow
        };

        await handler.Handle(new ProcessSessionStartedCommand(evt), CancellationToken.None);

        var auditLog = await auditRepo.GetBySessionAsync(evt.SessionId);
        Assert.NotNull(auditLog);
        Assert.Contains(auditLog!.Events, e => e.EventType == SessionEventType.SessionStarted);
        Assert.Contains(auditLog.Events, e =>
            e.EventType == SessionEventType.TeamRegistered && e.TeamRef == teamId);
        Assert.Contains(auditLog.Events.Single(e => e.EventType == SessionEventType.SessionStarted).Description, "Sesión iniciada");

        var ledger = await ledgerRepo.GetByTeamAndSessionAsync(teamId, evt.SessionId);
        Assert.NotNull(ledger);
        Assert.Equal(0, ledger!.TotalScore);
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

        public Task<int> CountFinishedAsync(Guid? operatorRef, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<MissionPlayCount>> GetMissionPlayCountsAsync(
            Guid? operatorRef, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MissionPlayCount>>([]);

        public Task<IReadOnlyList<OperatorSessionCount>> GetOperatorSessionCountsAsync(
            Guid? operatorRef, int limit, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OperatorSessionCount>>([]);

        public Task SaveAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            _logs[auditLog.SessionRef] = auditLog;
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
