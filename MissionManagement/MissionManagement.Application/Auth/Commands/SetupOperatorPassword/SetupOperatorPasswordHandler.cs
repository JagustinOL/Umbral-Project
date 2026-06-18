using MediatR;
using MissionManagement.Application.Common;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Auth.Commands.SetupOperatorPassword;

public sealed class SetupOperatorPasswordHandler : IRequestHandler<SetupOperatorPasswordCommand, AuthTokenResult>
{
    private readonly IIdentityService _identityService;
    private readonly IAuthService _authService;

    public SetupOperatorPasswordHandler(IIdentityService identityService, IAuthService authService)
    {
        _identityService = identityService;
        _authService = authService;
    }

    public async Task<AuthTokenResult> Handle(SetupOperatorPasswordCommand request, CancellationToken cancellationToken)
    {
        PasswordValidation.EnsureValid(request.Password);

        await _identityService.SetupOperatorPasswordAsync(
            request.Email.Trim(),
            request.SetupCode,
            request.Password,
            cancellationToken);

        return await _authService.AuthenticateAsync(
            request.Email.Trim(),
            request.Password,
            cancellationToken);
    }
}
