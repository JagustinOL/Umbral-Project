using MediatR;

namespace UserService.Application.Players.Commands.CreatePlayer;

public sealed record CreatePlayerCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password
) : IRequest<Guid>;
