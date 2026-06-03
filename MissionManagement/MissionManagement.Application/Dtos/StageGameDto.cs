namespace MissionManagement.Application.Dtos;

public sealed record StageGameDto(
    Guid Id,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    string Title);
