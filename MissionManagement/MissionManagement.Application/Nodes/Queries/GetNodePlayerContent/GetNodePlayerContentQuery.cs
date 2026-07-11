using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Nodes.Queries.GetNodePlayerContent;

public sealed record GetNodePlayerContentQuery(
    Guid MissionId,
    Guid NodeId
) : IRequest<PlayerNodeContentDto>;
