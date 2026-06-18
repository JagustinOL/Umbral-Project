using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Auth.Commands.SetupOperatorPassword;

public sealed record SetupOperatorPasswordCommand(
    string Email,
    string SetupCode,
    string Password
) : IRequest<AuthTokenResult>;
