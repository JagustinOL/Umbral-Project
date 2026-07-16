using ScoringAudit.Domain.Aggregates;
using ScoringAudit.Domain.Entities;
using ScoringAudit.Domain.Exceptions;
using Xunit;

namespace ScoringAudit.Domain.Tests.Aggregates;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_InitializesOpenAuditLogWithSessionContext()
    {
        var sessionId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow.AddMinutes(-5);

        var auditLog = AuditLog.Create(sessionId, missionId, operatorId, startedAtUtc);

        Assert.Equal(sessionId, auditLog.SessionRef);
        Assert.Equal(missionId, auditLog.MissionRef);
        Assert.Equal(operatorId, auditLog.OperatorRef);
        Assert.Equal(startedAtUtc, auditLog.StartedAtUtc);
        Assert.Equal("Open", auditLog.Status);
        Assert.False(auditLog.IsClosed);
    }

    [Fact]
    public void RecordEvent_AddsEventWhileAuditLogIsOpen()
    {
        var auditLog = CreateAuditLog();
        var eventId = Guid.NewGuid();

        auditLog.RecordEvent(SessionEventType.SessionStarted, eventId, "La sesión fue iniciada.");

        var recorded = Assert.Single(auditLog.Events);
        Assert.Equal(eventId, recorded.SourceEventId);
        Assert.Equal(SessionEventType.SessionStarted, recorded.EventType);
    }

    [Fact]
    public void Close_SealsAuditLogAndRejectsNewEvents()
    {
        var auditLog = CreateAuditLog();
        var endedAtUtc = DateTime.UtcNow;

        auditLog.Close("Cancelled", endedAtUtc);

        Assert.True(auditLog.IsClosed);
        Assert.Equal("Cancelled", auditLog.Status);
        Assert.Equal(endedAtUtc, auditLog.EndedAtUtc);
        Assert.Throws<ScoringDomainException>(() =>
            auditLog.RecordEvent(SessionEventType.SessionFinalized, Guid.NewGuid(), "No debe registrarse."));
    }

    private static AuditLog CreateAuditLog() =>
        AuditLog.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
}
