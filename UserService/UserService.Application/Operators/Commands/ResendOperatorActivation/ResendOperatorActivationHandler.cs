using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;

namespace UserService.Application.Operators.Commands.ResendOperatorActivation;

public sealed class ResendOperatorActivationHandler
    : IRequestHandler<ResendOperatorActivationCommand, ResendOperatorActivationResult>
{
    private readonly IIdentityService _identityService;
    private readonly IOperatorActivationMailer _activationMailer;
    private readonly ILogger<ResendOperatorActivationHandler> _logger;

    public ResendOperatorActivationHandler(
        IIdentityService identityService,
        IOperatorActivationMailer activationMailer,
        ILogger<ResendOperatorActivationHandler> logger)
    {
        _identityService = identityService;
        _activationMailer = activationMailer;
        _logger = logger;
    }

    public async Task<ResendOperatorActivationResult> Handle(
        ResendOperatorActivationCommand request,
        CancellationToken cancellationToken)
    {
        var credentials = await _identityService.RegenerateOperatorSetupCodeAsync(
            request.OperatorId,
            cancellationToken);

        try
        {
            await _activationMailer.SendActivationCodeAsync(
                credentials.Email,
                credentials.FirstName,
                credentials.SetupCode,
                credentials.TtlDays,
                cancellationToken);

            return new ResendOperatorActivationResult(credentials.OperatorId, credentials.Email, true);
        }
        catch (ExternalDependencyException ex)
        {
            _logger.LogError(
                ex,
                "Se regeneró el código del operador {OperatorId} pero falló el envío a {Email}.",
                credentials.OperatorId,
                credentials.Email);
            return new ResendOperatorActivationResult(credentials.OperatorId, credentials.Email, false);
        }
    }
}
