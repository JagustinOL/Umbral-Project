namespace MissionManagement.Application.Dtos;

public sealed record MissionNodeValidationDto(
    Guid NodeId,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    IReadOnlyList<string> ExpectedAnswers
);
