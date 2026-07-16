namespace SessionManagement.WebApi.Contracts.LiveSessions;

public sealed record SubmitTriviaAnswerRequest(
    Guid NodeId,
    string Answer,
    int QuestionIndex
);
