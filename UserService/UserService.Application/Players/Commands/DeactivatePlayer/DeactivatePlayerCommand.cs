using MediatR;

namespace UserService.Application.Players.Commands.DeactivatePlayer;

public sealed record DeactivatePlayerCommand(Guid PlayerId) : IRequest;
