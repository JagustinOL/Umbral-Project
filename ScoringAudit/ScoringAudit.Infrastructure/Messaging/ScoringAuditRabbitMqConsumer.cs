using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScoringAudit.Application.Events;
using Umbral.Shared.Messaging;
using Umbral.Shared.Messaging.IntegrationEvents;

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
            case nameof(EvidenceValidatedIntegrationEvent):
                var evidence = JsonSerializer.Deserialize<EvidenceValidatedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessEvidenceValidatedCommand(evidence), cancellationToken);
                break;

            case nameof(TeamRegisteredIntegrationEvent):
                var team = JsonSerializer.Deserialize<TeamRegisteredIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessTeamRegisteredCommand(team), cancellationToken);
                break;

            case nameof(SessionFinalizedIntegrationEvent):
                var finalized = JsonSerializer.Deserialize<SessionFinalizedIntegrationEvent>(envelope.Payload)!;
                await mediator.Send(new ProcessSessionFinalizedCommand(finalized), cancellationToken);
                break;
        }
    }
}
