namespace SessionManagement.Application.Common.Interfaces;

public sealed record PlayerNodeContentData(
    Guid NodeId,
    string NodeType,
    IReadOnlyList<PlayerTriviaQuestionData>? Questions,
    string? Instructions
);

public sealed record PlayerTriviaQuestionData(
    string Prompt,
    IReadOnlyList<string> Options
);
