using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Application.Evidence.Validation.Handlers;

public sealed class SessionActiveValidationHandler : EvidenceValidationHandlerBase
{
    protected override void Validate(EvidenceValidationContext context)
    {
        if (context.Session.Status != LiveSessionStatus.Active)
            throw new SessionDomainException(
                $"No se pueden aceptar evidencias en una sesión con estado '{context.Session.Status}'.");
    }
}
