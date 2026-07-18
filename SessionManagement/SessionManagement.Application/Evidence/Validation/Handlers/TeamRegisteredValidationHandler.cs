using SessionManagement.Domain.Exceptions;

namespace SessionManagement.Application.Evidence.Validation.Handlers;

public sealed class TeamRegisteredValidationHandler : EvidenceValidationHandlerBase
{
    protected override void Validate(EvidenceValidationContext context)
    {
        if (!context.Session.RegisteredTeamIds.Contains(context.TeamId))
            throw new SessionDomainException(
                $"El equipo {context.TeamId} no está registrado en la sesión {context.Session.Id}.");
    }
}
