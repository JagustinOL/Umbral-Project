namespace MissionManagement.Application.Dtos;

public sealed record TriviaNodeDto(
    Guid Id,
    Guid MissionId,
    Guid ParentNodeId,
    string NodeType,
    int ExecutionOrder,
    int BaseScore,
    IReadOnlyList<TriviaQuestionDto> Questions
);

