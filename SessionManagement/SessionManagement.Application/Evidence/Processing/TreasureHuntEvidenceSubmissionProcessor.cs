using SessionManagement.Application.Evidence.Validation;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Evidence.Processing;

public sealed class TreasureHuntEvidenceSubmissionProcessor : EvidenceSubmissionProcessor
{
    public TreasureHuntEvidenceSubmissionProcessor(EvidenceValidatorService validator) : base(validator) { }

    protected override NodeValidationType ExpectedType => NodeValidationType.TreasureHunt;

    protected override string NormalizePayload(string payload) =>
        payload.Trim().ToUpperInvariant();
}
