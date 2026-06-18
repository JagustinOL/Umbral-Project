using MissionManagement.Application.Nodes.Commands.AddTreasureHuntNode;
using MissionManagement.Application.Nodes.Commands.UpdateTreasureHuntNode;
using MissionManagement.Application.Nodes.Queries.GetTreasureHuntNodeById;
using MissionManagement.Domain.ValueObjects;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Contracts.TreasureHunts;

namespace MissionManagement.WebApi.Mapping;

public static class TreasureHuntMappings
{
    public static AddTreasureHuntNodeCommand ToCommand(
        this AddTreasureHuntNodeRequest body,
        MissionParentNodeRoute route) =>
        new(
            MissionId: route.MissionId,
            ParentNodeId: route.ParentNodeId,
            Instructions: body.Instructions,
            SecretCode: body.SecretCode,
            Destination: new GpsCoordinate(body.Destination.Latitude, body.Destination.Longitude),
            ExecutionOrder: body.ExecutionOrder);

    public static GetTreasureHuntNodeByIdQuery ToTreasureHuntByIdQuery(this MissionNodeRoute route) =>
        new(route.MissionId, route.NodeId);

    public static UpdateTreasureHuntNodeCommand ToCommand(
        this UpdateTreasureHuntNodeRequest body,
        MissionNodeRoute route) =>
        new(
            MissionId: route.MissionId,
            NodeId: route.NodeId,
            Instructions: body.Instructions,
            SecretCode: body.SecretCode,
            Destination: new GpsCoordinate(body.Destination.Latitude, body.Destination.Longitude));
}
