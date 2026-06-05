using MediatR;

namespace MissionManagement.Application.Missions.Commands.RevokeOperatorFromMission;

public sealed record RevokeOperatorFromMissionCommand(
    Guid MissionId,
    Guid OperatorId
) : IRequest;

