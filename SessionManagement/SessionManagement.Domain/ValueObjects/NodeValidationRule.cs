namespace SessionManagement.Domain.ValueObjects;

public enum NodeValidationType
{
    Trivia = 0,
    TreasureHunt = 1
}

public sealed record NodeValidationRule(
    Guid NodeId,
    int ExecutionOrder,
    NodeValidationType ValidationType,
    IReadOnlyList<string> ExpectedAnswers
);
