using MissionManagement.Application.Nodes.Commands.AddTriviaNode;
using MissionManagement.Application.Nodes.Commands.UpdateTriviaNode;
using MissionManagement.Application.Nodes.Queries.GetTriviaNodeById;
using MissionManagement.Domain.ValueObjects;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Contracts.Trivia;

namespace MissionManagement.WebApi.Mapping;

public static class TriviaMappings
{
    public static AddTriviaNodeCommand ToCommand(this AddTriviaNodeRequest body, MissionParentNodeRoute route) =>
        new(
            MissionId: route.MissionId,
            ParentNodeId: route.ParentNodeId,
            Questions: body.Questions
                .Select(q => new TriviaQuestion(q.Prompt, q.Options, q.CorrectOptionIndex))
                .ToList(),
            ExecutionOrder: body.ExecutionOrder,
            BaseScore: body.BaseScore);

    public static GetTriviaNodeByIdQuery ToTriviaByIdQuery(this MissionNodeRoute route) =>
        new(route.MissionId, route.NodeId);

    public static UpdateTriviaNodeCommand ToCommand(this UpdateTriviaNodeRequest body, MissionNodeRoute route) =>
        new(
            MissionId: route.MissionId,
            NodeId: route.NodeId,
            Questions: body.Questions
                .Select(q => new TriviaQuestion(q.Prompt, q.Options, q.CorrectOptionIndex))
                .ToList(),
            BaseScore: body.BaseScore);
}
