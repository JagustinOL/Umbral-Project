namespace MissionManagement.Application.Dtos;

public sealed record PlayerTriviaQuestionDto(
    string Prompt,
    IReadOnlyList<string> Options
);
