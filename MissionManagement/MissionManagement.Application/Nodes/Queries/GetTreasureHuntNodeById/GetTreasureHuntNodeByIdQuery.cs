using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Nodes.Queries.GetTreasureHuntNodeById;

public sealed record GetTreasureHuntNodeByIdQuery(
    Guid MissionId,
    Guid NodeId
) : IRequest<TreasureHuntNodeDto>;

