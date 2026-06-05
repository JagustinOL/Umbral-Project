namespace MissionManagement.WebApi.Contracts.Trivia;

public sealed record AddTriviaNodeRequest(
    List<TriviaQuestionRequest> Questions,
    int ExecutionOrder
);

