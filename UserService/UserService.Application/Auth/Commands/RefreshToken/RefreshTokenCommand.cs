using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokenResult>;
