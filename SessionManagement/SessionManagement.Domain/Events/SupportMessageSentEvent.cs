using SessionManagement.Domain.Common;

namespace SessionManagement.Domain.Events;

public sealed record SupportMessageSentEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;

    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid OperatorId { get; init; }
    public string Message { get; init; } = string.Empty;
}
