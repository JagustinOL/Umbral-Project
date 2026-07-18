namespace SessionManagement.Application.Dtos;

public sealed record TeamMissionProgressDto(
    Guid SessionId,
    Guid TeamId,
    bool IsMissionCompleted,
    int CompletedNodes,
    int TotalNodes,
    IReadOnlyList<MissionProgressNodeDto> Nodes);

public sealed record MissionProgressNodeDto(
    Guid NodeId,
    int ExecutionOrder,
    string NodeType,
    string Title,
    int BaseScore,
    bool IsCompleted);
