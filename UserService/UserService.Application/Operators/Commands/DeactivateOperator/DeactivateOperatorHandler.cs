using MediatR;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;
using System.Net.Http;

namespace UserService.Application.Operators.Commands.DeactivateOperator;

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
        bool hasActiveSessions;
        try
        {
            hasActiveSessions = await _sessionValidationService
                .HasActiveSessionsAsync(request.OperatorId, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            throw new ConflictException(
                $"No fue posible validar sesiones activas del operador con Id={request.OperatorId}. " +
                "La desactivación fue bloqueada por seguridad. " +
                $"Detalle técnico: {ex.Message}");
        }

        if (hasActiveSessions)
            throw new ConflictException(
                $"No se puede desactivar el operador con Id={request.OperatorId} porque tiene sesiones activas.");

        await _identityService.DeactivateOperatorAsync(request.OperatorId, cancellationToken);
    }
}

