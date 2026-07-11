namespace SessionManagement.Domain.ValueObjects;

public sealed record SubmissionResult(
    bool IsCorrect,
    Guid CurrentNodeId,
    Guid? NextNodeId,
    int AwardedPoints,
    int AnsweredQuestionIndex,
    int TotalQuestions,
    bool NodeCompleted
);
