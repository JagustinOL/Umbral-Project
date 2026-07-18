using SessionManagement.Application.Evidence.Validation;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Evidence.Processing;

public sealed class TriviaEvidenceSubmissionProcessor : EvidenceSubmissionProcessor
{
    public TriviaEvidenceSubmissionProcessor(EvidenceValidatorService validator) : base(validator) { }

    protected override NodeValidationType ExpectedType => NodeValidationType.Trivia;
}
