using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScoringAudit.Application.Events;
using ScoringAudit.Application.Messaging;
using ScoringAudit.Infrastructure.Messaging;

namespace ScoringAudit.Infrastructure.Messaging;

public sealed class ScoringAuditRabbitMqConsumer : RabbitMqConsumerHostedService
{
    public ScoringAuditRabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ScoringAuditRabbitMqConsumer> logger)
        : base(options, scopeFactory, logger, "scoring-audit.events", 
            "session.evidence.validated",
            "session.team.registered",
            "session.started",
            "session.hint.released",
            "session.penalty.applied",
            "session.team.completed",
            "session.finalized")
    {
    }

    protected override async Task HandleEnvelopeAsync(
        IServiceProvider services,
        DomainEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var mediator = services.GetRequiredService<IMediator>();

        switch (envelope.EventType)
        {
            case nameof(SessionStartedIntegrationEvent):
                var started = JsonSerializer.Deserialize<SessionStartedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessSessionStartedCommand(started), cancellationToken);
                break;

            case nameof(EvidenceValidatedIntegrationEvent):
                var evidence = JsonSerializer.Deserialize<EvidenceValidatedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessEvidenceValidatedCommand(evidence), cancellationToken);
                break;

            case nameof(TeamRegisteredIntegrationEvent):
                var team = JsonSerializer.Deserialize<TeamRegisteredIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessTeamRegisteredCommand(team), cancellationToken);
                break;

            case nameof(HintReleasedIntegrationEvent):
                var hintReleased = JsonSerializer.Deserialize<HintReleasedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessHintReleasedCommand(hintReleased), cancellationToken);
                break;

            case nameof(ManualPenaltyAppliedIntegrationEvent):
                var penalty = JsonSerializer.Deserialize<ManualPenaltyAppliedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessManualPenaltyCommand(penalty), cancellationToken);
                break;

            case nameof(TeamCompletedMissionIntegrationEvent):
                var completed = JsonSerializer.Deserialize<TeamCompletedMissionIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessTeamCompletedCommand(completed), cancellationToken);
                break;

            case nameof(SessionFinalizedIntegrationEvent):
                var finalized = JsonSerializer.Deserialize<SessionFinalizedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessSessionFinalizedCommand(finalized), cancellationToken);
                break;
        }
    }
}
