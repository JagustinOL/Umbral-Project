namespace SessionManagement.Application.Dtos;

public sealed record OperatorSessionBoardDto(
    Guid SessionId,
    string SessionStatus,
    IReadOnlyList<OperatorTeamBoardEntryDto> Teams);

public sealed record OperatorTeamBoardEntryDto(
    Guid TeamId,
    string? TeamName,
    string ParticipationStatus,
    Guid? CurrentNodeId,
    string? CurrentNodeType,
    int? CurrentExecutionOrder,
    string? CurrentGameLabel,
    bool IsMissionCompleted,
    IReadOnlyList<OperatorAvailableHintDto> AvailableHints,
    IReadOnlyList<OperatorReleasedHintSummaryDto> ReleasedHints);

public sealed record OperatorAvailableHintDto(
    Guid HintId,
    int Order,
    string Content,
    int PenaltyPoints,
    string? NodeType,
    string? NodePrompt);

public sealed record OperatorReleasedHintSummaryDto(
    Guid HintId,
    Guid MissionNodeId,
    int PenaltyPoints,
    DateTime ReleasedAtUtc,
    bool WasManualRelease,
    string? NodeType,
    string? NodePrompt);
