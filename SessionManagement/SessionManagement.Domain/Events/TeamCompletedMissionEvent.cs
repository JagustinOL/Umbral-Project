using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Events;

public sealed record TeamCompletedMissionEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public double ElapsedSeconds { get; init; }
}
