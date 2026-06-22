using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Players.Commands.DeactivatePlayer;

public sealed class DeactivatePlayerHandler : IRequestHandler<DeactivatePlayerCommand>
{
    private readonly IPlayerIdentityService _playerIdentityService;

    public DeactivatePlayerHandler(IPlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public async Task Handle(DeactivatePlayerCommand request, CancellationToken cancellationToken)
    {
        await _playerIdentityService.DeactivatePlayerAsync(request.PlayerId, cancellationToken);
    }
}
