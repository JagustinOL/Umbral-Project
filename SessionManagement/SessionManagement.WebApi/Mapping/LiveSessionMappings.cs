using SessionManagement.Application.LiveSessions.Commands.JoinSession;
using SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;
using SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;
using SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentStage;
using SessionManagement.WebApi.Contracts.LiveSessions;
using SessionManagement.WebApi.Contracts.Routes;

namespace SessionManagement.WebApi.Mapping;

public static class LiveSessionMappings
{
    public static JoinSessionCommand ToCommand(this JoinSessionRequest body) =>
        new(JoinCode: body.JoinCode, TeamId: body.TeamId);

    public static GetTeamCurrentStageQuery ToQuery(this LiveSessionTeamRoute route) =>
        new(SessionId: route.SessionId, TeamId: route.TeamId);

    public static SubmitTreasureHuntCodeCommand ToCommand(
        this SubmitTreasureHuntCodeRequest body,
        LiveSessionTeamRoute route) =>
        new(
            SessionId: route.SessionId,
            TeamId: route.TeamId,
            NodeId: body.NodeId,
            FoundCode: body.FoundCode);

    public static SubmitTriviaAnswerCommand ToCommand(
        this SubmitTriviaAnswerRequest body,
        LiveSessionTeamRoute route) =>
        new(
            SessionId: route.SessionId,
            TeamId: route.TeamId,
            NodeId: body.NodeId,
            Answer: body.Answer);
}
