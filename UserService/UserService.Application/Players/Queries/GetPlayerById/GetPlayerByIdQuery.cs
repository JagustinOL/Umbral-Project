using MediatR;
using UserService.Application.Common.Interfaces;

namespace UserService.Application.Players.Queries.GetPlayerById;

public sealed record GetPlayerByIdQuery(Guid PlayerId) : IRequest<PlayerIdentityDto>;
