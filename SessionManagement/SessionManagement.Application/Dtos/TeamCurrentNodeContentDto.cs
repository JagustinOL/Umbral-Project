namespace SessionManagement.Application.Dtos;

public sealed record TeamCurrentNodeContentDto(
    Guid NodeId,
    string NodeType,
    IReadOnlyList<PlayerTriviaQuestionDto>? Questions,
    string? Instructions,
    int CurrentQuestionIndex,
    int TotalQuestions
);
