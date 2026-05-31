namespace SessionManagement.Application.Dtos;

public sealed record TeamCurrentStageDto(
    Guid SessionId,
    Guid TeamId,
    Guid? CurrentNodeId,
    string? CurrentNodeType,
    int? CurrentExecutionOrder,
    bool IsCompleted
);

