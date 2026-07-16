using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Auth.Commands.AuthenticateUser;

public sealed record AuthenticateUserCommand(
    string Username,
    string Password) : IRequest<AuthTokenResult>;
