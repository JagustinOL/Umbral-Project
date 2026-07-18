using MissionManagement.Application.Nodes.Commands.AddRootNode;
using MissionManagement.Application.Nodes.Commands.DeleteNode;
using MissionManagement.Application.Nodes.Commands.UpdateNode;
using MissionManagement.Application.Nodes.Queries.GetGamesByStage;
using MissionManagement.Application.Nodes.Queries.GetNodesByMission;
using MissionManagement.WebApi.Contracts.Nodes;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Mapping;

public static class NodeMappings
{
    public static AddRootNodeCommand ToCommand(this AddRootNodeRequest body, MissionNodesRoute route) =>
        new(
            MissionId: route.MissionId,
            Title: body.Title,
            Description: body.Description,
            ExecutionOrder: body.ExecutionOrder);

    public static GetNodesByMissionQuery ToQuery(this MissionNodesRoute route) =>
        new(route.MissionId);

    public static GetGamesByStageQuery ToQuery(this MissionStageRoute route) =>
        new(route.MissionId, route.StageId);

    public static UpdateNodeCommand ToCommand(this UpdateNodeRequest body, MissionNodeRoute route) =>
        new(
            MissionId: route.MissionId,
            NodeId: route.NodeId,
            Title: body.Title,
            Description: body.Description);

    public static DeleteNodeCommand ToCommand(this MissionNodeRoute route) =>
        new(route.MissionId, route.NodeId);
}
