using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Common;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;

namespace UserService.Application.Operators.Commands.CreateOperator;

public sealed class CreateOperatorHandler : IRequestHandler<CreateOperatorCommand, CreateOperatorResult>
{
    private readonly IIdentityService _identityService;
    private readonly IOperatorActivationMailer _activationMailer;
    private readonly ILogger<CreateOperatorHandler> _logger;

    public CreateOperatorHandler(
        IIdentityService identityService,
        IOperatorActivationMailer activationMailer,
        ILogger<CreateOperatorHandler> logger)
    {
        _identityService = identityService;
        _activationMailer = activationMailer;
        _logger = logger;
    }

    public async Task<CreateOperatorResult> Handle(
        CreateOperatorCommand request,
        CancellationToken cancellationToken)
    {
        var credentials = await _identityService.CreateOperatorAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            cancellationToken);

        var emailSent = await TrySendActivationEmailAsync(credentials, cancellationToken);
        return new CreateOperatorResult(credentials.OperatorId, credentials.Email, emailSent);
    }

    private async Task<bool> TrySendActivationEmailAsync(
        OperatorSetupCredentials credentials,
        CancellationToken cancellationToken)
    {
        try
        {
            await _activationMailer.SendActivationCodeAsync(
                credentials.Email,
                credentials.FirstName,
                credentials.SetupCode,
                credentials.TtlDays,
                cancellationToken);
            return true;
        }
        catch (ExternalDependencyException ex)
        {
            _logger.LogError(
                ex,
                "Operador {OperatorId} creado pero falló el envío del correo a {Email}. Use reenvío.",
                credentials.OperatorId,
                credentials.Email);
            return false;
        }
    }
}
