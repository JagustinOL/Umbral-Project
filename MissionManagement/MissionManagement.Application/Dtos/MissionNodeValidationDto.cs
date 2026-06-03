namespace MissionManagement.Application.Dtos;

public sealed record MissionNodeValidationDto(
    Guid NodeId,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    string ExpectedValue
);

