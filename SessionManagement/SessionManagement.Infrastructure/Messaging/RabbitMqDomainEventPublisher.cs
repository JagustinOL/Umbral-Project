using System.Text.Json;
using Microsoft.Extensions.Logging;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Domain.Common;
using SessionManagement.Domain.Events;
using SessionManagement.Infrastructure.Messaging;

namespace SessionManagement.Infrastructure.Messaging;

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
            switch (domainEvent)
            {
                case EvidenceValidatedEvent evidence:
                    await _publisher.PublishAsync(
                        "session.evidence.validated",
                        new EvidenceValidatedIntegrationEvent
                        {
                            EventId = evidence.EventId,
                            OccurredOnUtc = evidence.OccurredOnUtc,
                            SessionId = evidence.SessionId,
                            EvidenceSubmissionId = evidence.EvidenceSubmissionId,
                            TeamId = evidence.TeamId,
                            MissionNodeId = evidence.MissionNodeId,
                            NodeType = evidence.NodeType,
                            BaseScore = evidence.BaseScore,
                            DifficultyMultiplier = evidence.DifficultyMultiplier,
                            ElapsedSeconds = evidence.ElapsedSeconds
                        },
                        cancellationToken);
                    break;

                case TeamRegisteredEvent teamRegistered:
                    await _publisher.PublishAsync(
                        "session.team.registered",
                        new TeamRegisteredIntegrationEvent
                        {
                            EventId = teamRegistered.EventId,
                            SessionId = teamRegistered.SessionId,
                            TeamId = teamRegistered.TeamId,
                            TeamName = string.IsNullOrWhiteSpace(teamRegistered.TeamName)
                                ? $"Team-{teamRegistered.TeamId:N}".Substring(0, 12)
                                : teamRegistered.TeamName
                        },
                        cancellationToken);
                    break;

                case SessionFinalizedEvent finalized:
                    await _publisher.PublishAsync(
                        "session.finalized",
                        new SessionFinalizedIntegrationEvent
                        {
                            EventId = finalized.EventId,
                            SessionId = finalized.SessionId,
                            ParticipatingTeamIds = finalized.ParticipatingTeamIds
                        },
                        cancellationToken);
                    break;

                default:
                    _logger.LogInformation(
                        "Evento de dominio publicado (log): {EventType} {Payload}",
                        domainEvent.GetType().Name,
                        JsonSerializer.Serialize(domainEvent));
                    break;
            }
        }
    }
}
