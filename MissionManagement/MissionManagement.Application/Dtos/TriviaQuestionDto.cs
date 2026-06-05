namespace MissionManagement.Application.Dtos;

public sealed record TriviaQuestionDto(
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex
);

