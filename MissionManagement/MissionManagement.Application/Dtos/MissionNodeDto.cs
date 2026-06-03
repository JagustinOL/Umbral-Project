namespace MissionManagement.Application.Dtos;

public sealed record MissionNodeDto(
    Guid Id,
    string Title,
    string Description,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    Guid? ParentNodeId
);

