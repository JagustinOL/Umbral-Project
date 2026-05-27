using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Nodes.Queries.GetNodesByMission;

public sealed record GetNodesByMissionQuery(Guid MissionId) : IRequest<IReadOnlyList<MissionNodeDto>>;

