using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Auth.Commands.SetupOperatorPassword;

public sealed record SetupOperatorPasswordCommand(
    string Email,
    string SetupCode,
    string Password
) : IRequest<AuthTokenResult>;
