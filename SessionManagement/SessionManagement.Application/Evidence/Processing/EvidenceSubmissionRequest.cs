using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Evidence.Processing;

public sealed record EvidenceSubmissionRequest(
    Guid TeamId,
    Guid NodeId,
    string Payload,
    IReadOnlyList<NodeValidationRule> ValidationRules);
