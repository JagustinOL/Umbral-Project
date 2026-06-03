using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Missions.Queries.GetMissions;

public sealed record GetMissionsQuery() : IRequest<IReadOnlyList<MissionDto>>;

