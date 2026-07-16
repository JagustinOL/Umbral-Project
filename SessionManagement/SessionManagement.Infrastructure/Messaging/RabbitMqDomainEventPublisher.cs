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
    private readonly ILiveSessionRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<RabbitMqDomainEventPublisher> _logger;

    public RabbitMqDomainEventPublisher(
        IRabbitMqPublisher publisher,
        ILiveSessionRealtimeNotifier realtimeNotifier,
        ILogger<RabbitMqDomainEventPublisher> logger)
    {
        _publisher = publisher;
        _realtimeNotifier = realtimeNotifier;
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
                            NodeTitle = evidence.NodeTitle,
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
                                ? $"Team-{teamRegistered.TeamId:N}"[..12]
                                : teamRegistered.TeamName
                        },
                        cancellationToken);
                    break;

                case SessionStartedEvent started:
                    await _publisher.PublishAsync(
                        "session.started",
                        new SessionStartedIntegrationEvent
                        {
                            EventId = started.EventId,
                            SessionId = started.SessionId,
                            MissionRef = started.MissionRef,
                            OperatorRef = started.OperatorRef,
                            ParticipatingTeamIds = started.ParticipatingTeamIds,
                            StartedAtUtc = started.StartedAtUtc
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
                            ParticipatingTeamIds = finalized.ParticipatingTeamIds,
                            MissionRef = finalized.MissionRef,
                            OperatorRef = finalized.OperatorRef,
                            FinalizedAtUtc = finalized.FinalizedAtUtc,
                            Status = finalized.Status
                        },
                        cancellationToken);
                    break;

                case HintReleasedEvent hint:
                    await _publisher.PublishAsync(
                        "session.hint.released",
                        new HintReleasedIntegrationEvent
                        {
                            EventId = hint.EventId,
                            SessionId = hint.SessionId,
                            TeamId = hint.TeamId,
                            HintId = hint.HintId,
                            MissionNodeId = hint.MissionNodeId,
                            NodeType = hint.NodeType,
                            NodeTitle = hint.NodeTitle,
                            HintOrder = hint.HintOrder,
                            PenaltyPoints = hint.PenaltyPoints,
                            WasManualRelease = hint.WasManualRelease
                        },
                        cancellationToken);
                    await _realtimeNotifier.NotifyHintReleasedAsync(
                        hint.SessionId, hint.TeamId, hint.HintId, hint.MissionNodeId, hint.PenaltyPoints, cancellationToken);
                    break;

                case ManualPenaltyAppliedEvent penalty:
                    await _publisher.PublishAsync(
                        "session.penalty.applied",
                        new ManualPenaltyAppliedIntegrationEvent
                        {
                            EventId = penalty.EventId,
                            SessionId = penalty.SessionId,
                            TeamId = penalty.TeamId,
                            OperatorRef = penalty.OperatorRef,
                            PenaltyPoints = penalty.PenaltyPoints,
                            Reason = penalty.Reason
                        },
                        cancellationToken);
                    await _realtimeNotifier.NotifyManualPenaltyAsync(
                        penalty.SessionId, penalty.TeamId, penalty.PenaltyPoints, penalty.Reason, cancellationToken);
                    break;

                case TeamCompletedMissionEvent completed:
                    await _publisher.PublishAsync(
                        "session.team.completed",
                        new TeamCompletedMissionIntegrationEvent
                        {
                            EventId = completed.EventId,
                            SessionId = completed.SessionId,
                            TeamId = completed.TeamId,
                            CompletedAtUtc = completed.CompletedAtUtc,
                            ElapsedSeconds = completed.ElapsedSeconds
                        },
                        cancellationToken);
                    await _realtimeNotifier.NotifyTeamProgressUpdatedAsync(
                        completed.SessionId, completed.TeamId, null, null, true, cancellationToken);
                    break;

                case SessionStateChangedEvent stateChanged:
                    await _realtimeNotifier.NotifySessionStateChangedAsync(
                        stateChanged.SessionId,
                        stateChanged.PreviousStatus.ToString(),
                        stateChanged.NewStatus.ToString(),
                        stateChanged.Reason,
                        cancellationToken);
                    break;

                case SessionJoinRequestCreatedEvent joinCreated:
                    await _realtimeNotifier.NotifyJoinRequestReceivedAsync(
                        joinCreated.SessionId, joinCreated.TeamId, joinCreated.RequestId, cancellationToken);
                    break;

                case SessionJoinRequestResolvedEvent joinResolved:
                    await _realtimeNotifier.NotifyJoinRequestResolvedAsync(
                        joinResolved.SessionId,
                        joinResolved.TeamId,
                        joinResolved.RequestId,
                        joinResolved.Decision.ToString(),
                        cancellationToken);
                    break;

                case SupportMessageSentEvent support:
                    await _realtimeNotifier.NotifySupportMessageAsync(
                        support.SessionId, support.TeamId, support.Message, cancellationToken);
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
