using MissionManagement.Domain.Common;

namespace MissionManagement.Application.Common.Interfaces;

public interface IDomainEventPublisher
{
    Task PublishAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
