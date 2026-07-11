namespace MissionManagement.Application.Dtos;

public sealed record PlayerNodeContentDto(
    Guid NodeId,
    string NodeType,
    IReadOnlyList<PlayerTriviaQuestionDto>? Questions,
    string? Instructions
);
