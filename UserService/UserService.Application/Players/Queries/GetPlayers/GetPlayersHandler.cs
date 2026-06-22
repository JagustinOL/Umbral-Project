using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Players.Queries.GetPlayers;

public sealed class GetPlayersHandler : IRequestHandler<GetPlayersQuery, IReadOnlyList<PlayerIdentityDto>>
{
    private readonly IPlayerIdentityService _playerIdentityService;

    public GetPlayersHandler(IPlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public async Task<IReadOnlyList<PlayerIdentityDto>> Handle(GetPlayersQuery request, CancellationToken cancellationToken)
    {
        return await _playerIdentityService.GetPlayersAsync(cancellationToken);
    }
}
