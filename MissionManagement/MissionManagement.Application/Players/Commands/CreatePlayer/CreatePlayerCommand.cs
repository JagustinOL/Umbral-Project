using MediatR;

namespace MissionManagement.Application.Players.Commands.CreatePlayer;

public sealed record CreatePlayerCommand(
    string FirstName,
    string LastName,
    string Email
) : IRequest<Guid>;
