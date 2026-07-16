using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthTokenResult>
{
    private readonly IAuthService _authService;

    public RefreshTokenHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<AuthTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken) =>
        _authService.RefreshAsync(request.RefreshToken, cancellationToken);
}
