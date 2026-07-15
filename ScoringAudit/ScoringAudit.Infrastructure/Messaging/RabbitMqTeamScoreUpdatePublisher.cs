using ScoringAudit.Application.Messaging;

namespace ScoringAudit.Infrastructure.Messaging;

public sealed class RabbitMqTeamScoreUpdatePublisher : ITeamScoreUpdatePublisher
{
    private readonly IRabbitMqPublisher _publisher;

    public RabbitMqTeamScoreUpdatePublisher(IRabbitMqPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task PublishAsync(TeamScoreUpdatedIntegrationEvent message, CancellationToken cancellationToken = default) =>
        _publisher.PublishAsync(TeamScoreUpdateRouting.RoutingKey, message, cancellationToken);
}
