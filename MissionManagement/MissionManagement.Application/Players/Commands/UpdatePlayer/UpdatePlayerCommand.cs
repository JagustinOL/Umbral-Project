using MediatR;

namespace MissionManagement.Application.Players.Commands.UpdatePlayer;

public sealed record UpdatePlayerCommand(
    Guid PlayerId,
    string FirstName,
    string LastName,
    string Email
) : IRequest;
