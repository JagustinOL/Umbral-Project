using UserService.Application.Auth.Commands.AuthenticateUser;
using UserService.Application.Auth.Commands.RefreshToken;
using UserService.Application.Auth.Commands.SetupOperatorPassword;
using UserService.WebApi.Contracts.Auth;

namespace UserService.WebApi.Mapping;

public static class AuthMappings
{
    public static AuthenticateUserCommand ToCommand(this AuthenticateRequest body) =>
        new(body.Username, body.Password);

    public static RefreshTokenCommand ToCommand(this RefreshTokenRequest body) =>
        new(body.RefreshToken);

    public static SetupOperatorPasswordCommand ToCommand(this SetupOperatorPasswordRequest body) =>
        new(body.Email, body.SetupCode, body.Password);
}
