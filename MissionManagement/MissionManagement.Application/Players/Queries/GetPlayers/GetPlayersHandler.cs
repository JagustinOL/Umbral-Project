using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Players.Queries.GetPlayers;

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
