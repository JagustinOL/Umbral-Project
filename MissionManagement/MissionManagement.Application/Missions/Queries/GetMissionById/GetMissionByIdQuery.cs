using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Missions.Queries.GetMissionById;

public sealed record GetMissionByIdQuery(Guid Id) : IRequest<MissionDto>;

