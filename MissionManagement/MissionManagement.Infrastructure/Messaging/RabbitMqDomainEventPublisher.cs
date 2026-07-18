using System.Text.Json;
using Microsoft.Extensions.Logging;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Domain.Common;
using MissionManagement.Infrastructure.Messaging;

namespace MissionManagement.Infrastructure.Messaging;

public sealed class RabbitMqDomainEventPublisher : IDomainEventPublisher
{
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<RabbitMqDomainEventPublisher> _logger;

    public RabbitMqDomainEventPublisher(
        IRabbitMqPublisher publisher,
        ILogger<RabbitMqDomainEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation(
                "Publicando evento de dominio {EventType}: {Payload}",
                domainEvent.GetType().Name,
                JsonSerializer.Serialize(domainEvent));

            if (domainEvent is MissionManagement.Domain.Events.MissionActivatedEvent activated)
            {
                await _publisher.PublishAsync(
                    "mission.activated",
                    new { activated.MissionId, activated.OccurredOnUtc },
                    cancellationToken);
            }
        }
    }
}
