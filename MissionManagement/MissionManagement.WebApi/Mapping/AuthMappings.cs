using MissionManagement.Application.Auth.Commands.AuthenticateUser;
using MissionManagement.Application.Auth.Commands.SetupOperatorPassword;
using MissionManagement.WebApi.Contracts.Auth;

namespace MissionManagement.WebApi.Mapping;

public static class AuthMappings
{
    public static AuthenticateUserCommand ToCommand(this AuthenticateRequest body) =>
        new(body.Username, body.Password);

    public static SetupOperatorPasswordCommand ToCommand(this SetupOperatorPasswordRequest body) =>
        new(body.Email, body.SetupCode, body.Password);
}
