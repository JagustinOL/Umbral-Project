namespace SessionManagement.Infrastructure.Messaging;

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
    public Guid MissionRef { get; init; }
    public Guid OperatorRef { get; init; }
    public DateTime FinalizedAtUtc { get; init; }
    public string Status { get; init; } = "Finalized";
}

public sealed record SessionStartedIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public Guid MissionRef { get; init; }
    public Guid OperatorRef { get; init; }
    public IReadOnlyList<Guid> ParticipatingTeamIds { get; init; } = [];
    public DateTime StartedAtUtc { get; init; }
}

public sealed record HintReleasedIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid HintId { get; init; }
    public Guid MissionNodeId { get; init; }
    public int PenaltyPoints { get; init; }
    public bool WasManualRelease { get; init; }
}

public sealed record ManualPenaltyAppliedIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public Guid OperatorRef { get; init; }
    public int PenaltyPoints { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record TeamCompletedMissionIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid SessionId { get; init; }
    public Guid TeamId { get; init; }
    public DateTime CompletedAtUtc { get; init; }
    public double ElapsedSeconds { get; init; }
}

public sealed record DomainEventEnvelope
{
    public string EventType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime OccurredOnUtc { get; init; }
}
