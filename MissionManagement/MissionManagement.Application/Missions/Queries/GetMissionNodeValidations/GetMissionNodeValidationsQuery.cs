using MediatR;
using MissionManagement.Application.Dtos;

namespace MissionManagement.Application.Missions.Queries.GetMissionNodeValidations;

public sealed record GetMissionNodeValidationsQuery(Guid MissionId)
    : IRequest<IReadOnlyList<MissionNodeValidationDto>>;
