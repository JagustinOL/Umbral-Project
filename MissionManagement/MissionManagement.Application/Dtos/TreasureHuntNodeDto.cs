namespace MissionManagement.Application.Dtos;

public sealed record TreasureHuntNodeDto(
    Guid Id,
    Guid MissionId,
    Guid ParentNodeId,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    string Instructions,
    string SecretCode,
    GpsCoordinateDto Destination
);

