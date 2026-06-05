using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Auth.Commands.AuthenticateUser;

public sealed class AuthenticateUserHandler : IRequestHandler<AuthenticateUserCommand, AuthTokenResult>
{
    private readonly IAuthService _authService;

    public AuthenticateUserHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<AuthTokenResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ArgumentException("Username is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        return _authService.AuthenticateAsync(
            request.Username.Trim(),
            request.Password,
            cancellationToken);
    }
}
