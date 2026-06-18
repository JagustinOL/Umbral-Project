namespace Umbral.Shared.Messaging.IntegrationEvents;

public sealed record EvidenceValidatedIntegrationEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredOnUtc { get; init; }
    public Guid SessionId { get; init; }
    public Guid EvidenceSubmissionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid MissionNodeId { get; init; }
    public string NodeType { get; init; } = string.Empty;
    public int BaseScore { get; init; }
    public decimal DifficultyMultiplier { get; init; }
    public double ElapsedSeconds { get; init; }
}

public sealed record TeamRegisteredIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = string.Empty;
}

public sealed record SessionFinalizedIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public IReadOnlyList<Guid> ParticipatingTeamIds { get; init; } = [];
}

public sealed record DomainEventEnvelope
{
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime OccurredOnUtc { get; init; }
}
