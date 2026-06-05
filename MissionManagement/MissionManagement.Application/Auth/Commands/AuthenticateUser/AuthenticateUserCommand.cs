using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Auth.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(
    string Username,
    string Password) : IRequest<AuthTokenResult>;
