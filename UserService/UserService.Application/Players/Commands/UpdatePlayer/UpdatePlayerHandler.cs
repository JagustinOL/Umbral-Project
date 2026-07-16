using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Players.Commands.UpdatePlayer;

public sealed class UpdatePlayerHandler : IRequestHandler<UpdatePlayerCommand>
{
    private readonly IPlayerIdentityService _playerIdentityService;

    public UpdatePlayerHandler(IPlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public async Task Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
    {
        await _playerIdentityService.UpdatePlayerAsync(
            request.PlayerId,
            request.FirstName,
            request.LastName,
            request.Email,
            cancellationToken);
    }
}
