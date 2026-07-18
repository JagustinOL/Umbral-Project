namespace SessionManagement.Application.Dtos;

public sealed record TeamReleasedHintDto(
    Guid HintId,
    Guid MissionNodeId,
    int Order,
    string Content,
    int PenaltyPoints,
    DateTime ReleasedAtUtc,
    bool WasManualRelease,
    string? NodeType,
    string? NodePrompt);
