using MediatR;

namespace MissionManagement.Application.Players.Commands.DeactivatePlayer;

public sealed record DeactivatePlayerCommand(Guid PlayerId) : IRequest;
