using Microsoft.Extensions.Logging;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Domain.Common;

namespace MissionManagement.Infrastructure.Messaging;

public sealed class LoggingDomainEventPublisher : IDomainEventPublisher
{
    private readonly ILogger<LoggingDomainEventPublisher> _logger;

    public LoggingDomainEventPublisher(ILogger<LoggingDomainEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation(
                "Domain event published: {EventType} ({EventId}) at {OccurredOnUtc:O}",
                domainEvent.GetType().Name,
                domainEvent.EventId,
                domainEvent.OccurredOnUtc);
        }

        return Task.CompletedTask;
    }
}
