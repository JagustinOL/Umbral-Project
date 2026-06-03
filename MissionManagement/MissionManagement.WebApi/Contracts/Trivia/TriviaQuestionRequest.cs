namespace MissionManagement.WebApi.Contracts.Trivia;

public sealed record TriviaQuestionRequest(
    string Prompt,
    List<string> Options,
    int CorrectOptionIndex
);

