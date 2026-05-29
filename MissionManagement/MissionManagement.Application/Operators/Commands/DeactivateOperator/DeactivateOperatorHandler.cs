using MediatR;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;

namespace MissionManagement.Application.Operators.Commands.DeactivateOperator;

public sealed class DeactivateOperatorHandler : IRequestHandler<DeactivateOperatorCommand>
{
    private readonly IIdentityService _identityService;
    private readonly ISessionValidationService _sessionValidationService;

    public DeactivateOperatorHandler(
        IIdentityService identityService,
        ISessionValidationService sessionValidationService)
    {
        _identityService = identityService;
        _sessionValidationService = sessionValidationService;
    }

    public async Task Handle(DeactivateOperatorCommand request, CancellationToken cancellationToken)
    {
        var hasActiveSessions = await _sessionValidationService
            .HasActiveSessionsAsync(request.OperatorId, cancellationToken);

        if (hasActiveSessions)
            throw new ConflictException(
                $"No se puede desactivar el operador con Id={request.OperatorId} porque tiene sesiones activas.");

        await _identityService.DeactivateOperatorAsync(request.OperatorId, cancellationToken);
    }
}

