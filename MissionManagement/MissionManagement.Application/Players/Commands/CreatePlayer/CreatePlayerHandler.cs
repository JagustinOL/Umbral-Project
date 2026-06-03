using MediatR;
using MissionManagement.Application.Common;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Players.Commands.CreatePlayer;

public sealed class CreatePlayerHandler : IRequestHandler<CreatePlayerCommand, Guid>
{
    private readonly IPlayerIdentityService _playerIdentityService;

    public CreatePlayerHandler(IPlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public async Task<Guid> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        PasswordValidation.EnsureValid(request.Password);

        return await _playerIdentityService.CreatePlayerAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            cancellationToken);
    }
}
