using MediatR;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Application.Players.Queries.GetPlayerById;

public sealed record GetPlayerByIdQuery(Guid PlayerId) : IRequest<PlayerIdentityDto>;
