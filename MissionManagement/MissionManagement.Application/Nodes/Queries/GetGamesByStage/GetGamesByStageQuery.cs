using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Nodes.Queries.GetGamesByStage;

public sealed record GetGamesByStageQuery(Guid MissionId, Guid StageId) : IRequest<IReadOnlyList<StageGameDto>>;
