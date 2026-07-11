namespace SessionManagement.Application.Dtos;

public sealed record SubmissionResultDto(
    bool IsCorrect,
    Guid CurrentNodeId,
    Guid? NextNodeId,
    int AwardedPoints,
    int AnsweredQuestionIndex,
    int TotalQuestions,
    bool NodeCompleted
);
