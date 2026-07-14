using ScoringAudit.Application.Events;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Repositories;
using Xunit;

namespace ScoringAudit.Application.Tests.Events;

public sealed class ProcessSessionStartedHandlerTests
{
    [Fact]
    public async Task Handle_CreatesAuditLogAndRecordsSessionStarted()
    {
        var repository = new InMemoryAuditLogRepository();
        var handler = new ProcessSessionStartedHandler(repository);
        var evt = new SessionStartedIntegrationEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            MissionRef = Guid.NewGuid(),
            OperatorRef = Guid.NewGuid(),
            StartedAtUtc = DateTime.UtcNow
        };

        await handler.Handle(new ProcessSessionStartedCommand(evt), CancellationToken.None);

        var auditLog = await repository.GetBySessionAsync(evt.SessionId);
        Assert.NotNull(auditLog);
        Assert.Equal(SessionEventType.SessionStarted, Assert.Single(auditLog!.Events).EventType);
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
