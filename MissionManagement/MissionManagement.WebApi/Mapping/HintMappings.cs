using MissionManagement.Application.Hints.Commands.AddHint;
using MissionManagement.Application.Hints.Commands.DeleteHint;
using MissionManagement.Application.Hints.Commands.UpdateHint;
using MissionManagement.Application.Hints.Queries.GetHintsByNode;
using MissionManagement.WebApi.Contracts.Hints;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Mapping;

public static class HintMappings
{
    public static AddHintCommand ToCommand(this AddHintRequest body, HintParentRoute route) =>
        new(MissionId: route.MissionId, NodeId: route.NodeId, Content: body.Content);

    public static GetHintsByNodeQuery ToQuery(this HintParentRoute route) =>
        new(route.MissionId, route.NodeId);

    public static UpdateHintCommand ToCommand(this UpdateHintRequest body, HintRoute route) =>
        new(
            MissionId: route.MissionId,
            NodeId: route.NodeId,
            HintId: route.HintId,
            Content: body.Content);

    public static DeleteHintCommand ToCommand(this HintRoute route) =>
        new(route.MissionId, route.NodeId, route.HintId);
}
