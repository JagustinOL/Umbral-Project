namespace MissionManagement.WebApi.Contracts.Trivia;

public sealed record UpdateTriviaNodeRequest(
    List<TriviaQuestionRequest> Questions,
    int BaseScore
);
