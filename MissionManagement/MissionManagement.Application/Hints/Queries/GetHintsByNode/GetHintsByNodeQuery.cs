using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Hints.Queries.GetHintsByNode;

public sealed record GetHintsByNodeQuery(
    Guid MissionId,
    Guid NodeId
) : IRequest<IReadOnlyList<HintDto>>;

