namespace SessionManagement.Application.Evidence.Validation;

public abstract class EvidenceValidationHandlerBase : IEvidenceValidationHandler
{
    private IEvidenceValidationHandler? _next;

    public IEvidenceValidationHandler SetNext(IEvidenceValidationHandler next)
    {
        _next = next;
        return next;
    }

    public void Handle(EvidenceValidationContext context)
    {
        Validate(context);
        _next?.Handle(context);
    }

    protected abstract void Validate(EvidenceValidationContext context);
}
