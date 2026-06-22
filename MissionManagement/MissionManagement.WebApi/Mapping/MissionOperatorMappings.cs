using MissionManagement.Application.Missions.Commands.AssignOperatorToMission;
using MissionManagement.Application.Missions.Commands.RevokeOperatorFromMission;
using MissionManagement.WebApi.Contracts.MissionOperators;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Mapping;

public static class MissionOperatorMappings
{
    public static AssignOperatorToMissionCommand ToCommand(
        this AssignOperatorToMissionRequest body,
        MissionNodesRoute route) =>
        new(MissionId: route.MissionId, OperatorId: body.OperatorId);

    public static RevokeOperatorFromMissionCommand ToCommand(this MissionOperatorRoute route) =>
        new(MissionId: route.MissionId, OperatorId: route.OperatorId);
}
