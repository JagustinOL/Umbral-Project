using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Players.Queries.GetPlayers;

public sealed record GetPlayersQuery : IRequest<IReadOnlyList<PlayerIdentityDto>>;
