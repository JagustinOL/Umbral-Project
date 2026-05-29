using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Nodes.Queries.GetTriviaNodeById;

public sealed record GetTriviaNodeByIdQuery(
    Guid MissionId,
    Guid NodeId
) : IRequest<TriviaNodeDto>;

