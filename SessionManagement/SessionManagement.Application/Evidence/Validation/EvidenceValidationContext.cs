using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Evidence.Validation;

public sealed class EvidenceValidationContext
{
    public required LiveSession Session { get; init; }
    public required Guid TeamId { get; init; }
    public required Guid NodeId { get; init; }
    public required string Payload { get; init; }
    public required NodeValidationType ExpectedType { get; init; }
    public required IReadOnlyList<NodeValidationRule> ValidationRules { get; init; }
    public int? QuestionIndex { get; init; }

    public NodeValidationRule? CurrentRule { get; set; }
    public int ResolvedQuestionIndex { get; set; }
    public bool IsCorrect { get; set; }
    public string? RejectionReason { get; set; }
}
