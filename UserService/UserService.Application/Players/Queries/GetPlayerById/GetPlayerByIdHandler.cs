using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Players.Queries.GetPlayerById;

public sealed class GetPlayerByIdHandler : IRequestHandler<GetPlayerByIdQuery, PlayerIdentityDto>
{
    private readonly IPlayerIdentityService _playerIdentityService;

    public GetPlayerByIdHandler(IPlayerIdentityService playerIdentityService)
    {
        _playerIdentityService = playerIdentityService;
    }

    public async Task<PlayerIdentityDto> Handle(GetPlayerByIdQuery request, CancellationToken cancellationToken)
    {
        return await _playerIdentityService.GetPlayerByIdAsync(request.PlayerId, cancellationToken);
    }
}
