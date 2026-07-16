using SessionManagement.Domain.Common;
using SessionManagement.Domain.Entities;

namespace SessionManagement.Domain.Events;

public sealed record SessionJoinRequestResolvedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid RequestId { get; init; }
    public JoinRequestStatus Decision { get; init; }
    public Guid OperatorId { get; init; }
}
