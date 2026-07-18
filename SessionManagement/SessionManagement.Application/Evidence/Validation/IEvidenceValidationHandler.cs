namespace SessionManagement.Application.Evidence.Validation;

public interface IEvidenceValidationHandler
{
    IEvidenceValidationHandler SetNext(IEvidenceValidationHandler next);
    void Handle(EvidenceValidationContext context);
}
