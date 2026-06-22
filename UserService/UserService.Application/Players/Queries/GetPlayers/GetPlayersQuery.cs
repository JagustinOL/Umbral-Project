using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Players.Queries.GetPlayers;

public sealed record GetPlayersQuery : IRequest<IReadOnlyList<PlayerIdentityDto>>;
