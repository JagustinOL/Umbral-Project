using SessionManagement.Domain.Common;

namespace SessionManagement.Application.Common.Interfaces;

public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
