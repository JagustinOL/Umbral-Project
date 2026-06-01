using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.OperatorSessions.Queries.GetOperatorAssignedMissions;

public sealed record GetOperatorAssignedMissionsQuery(
    Guid OperatorId
) : IRequest<IReadOnlyList<OperatorAssignedMissionDto>>;

