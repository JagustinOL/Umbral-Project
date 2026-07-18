using SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;
using SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;
using SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorAssignedMissions;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorOpenSessions;
using SessionManagement.Application.OperatorSessions.Queries.GetSessionTeams;
using SessionManagement.Application.OperatorSessions.Queries.OperatorHasActiveSessions;
using SessionManagement.Application.OperatorSessions.Queries.OperatorIsSupervisingMission;
using SessionManagement.WebApi.Contracts.OperatorSessions;
using SessionManagement.WebApi.Contracts.Routes;

namespace SessionManagement.WebApi.Mapping;

public static class OperatorSessionMappings
{
    public static OperatorHasActiveSessionsQuery ToHasActiveSessionsQuery(this OperatorRoute route) =>
        new(route.OperatorId);

    public static OperatorIsSupervisingMissionQuery ToIsSupervisingMissionQuery(this OperatorMissionRoute route) =>
        new(route.OperatorId, route.MissionId);

    public static GetOperatorAssignedMissionsQuery ToAssignedMissionsQuery(this OperatorRoute route) =>
        new(route.OperatorId);

    public static GetOperatorOpenSessionsQuery ToOpenSessionsQuery(this OperatorRoute route) =>
        new(route.OperatorId);

    public static CreateLiveSessionCommand ToCreateLiveSessionCommand(
        this CreateLiveSessionRequest body,
        OperatorRoute route) =>
        new(OperatorId: route.OperatorId, MissionId: body.MissionId);

    public static GetSessionTeamsQuery ToSessionTeamsQuery(this OperatorSessionRoute route) =>
        new(OperatorId: route.OperatorId, SessionId: route.SessionId);

    public static StartLiveSessionCommand ToStartLiveSessionCommand(this OperatorSessionRoute route) =>
        new(OperatorId: route.OperatorId, SessionId: route.SessionId);

    public static FinalizeLiveSessionCommand ToFinalizeLiveSessionCommand(this OperatorSessionRoute route) =>
        new(route.OperatorId, route.SessionId);

    public static CancelLiveSessionCommand ToCancelLiveSessionCommand(this OperatorSessionRoute route) =>
        new(route.OperatorId, route.SessionId);
}
