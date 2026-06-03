using MediatR;

namespace MissionManagement.Application.Missions.Commands.AssignOperatorToMission;

public sealed record AssignOperatorToMissionCommand(
    Guid MissionId,
    Guid OperatorId
) : IRequest;

