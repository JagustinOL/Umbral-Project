using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Events;

public sealed record SessionJoinRequestCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid RequestId { get; init; }
}
