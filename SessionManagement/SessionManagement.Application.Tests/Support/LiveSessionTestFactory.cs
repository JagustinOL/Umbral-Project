using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Tests.Support;

internal static class LiveSessionTestFactory
{
    public static readonly Guid DefaultTeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid TriviaNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid TreasureNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static LiveSession BuildPendingSession(Guid? operatorId = null)
    {
        var op = operatorId ?? Guid.NewGuid();
        var missionRef = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionRef,
            op,
            [new AllowedNode(TriviaNodeId, "Trivia", 100)],
            1.0m);
        session.RegisterTeam(DefaultTeamId);
        return session;
    }

    public static LiveSession BuildActiveSession()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            1.0m);
        session.RegisterTeam(DefaultTeamId);
        session.BeginPreparation();
        session.Start();
        return session;
    }

    public static IReadOnlyList<MissionNodeValidationData> DefaultValidationData() =>
    [
        new MissionNodeValidationData(TriviaNodeId, "Trivia", 1, 100, "Bogota"),
        new MissionNodeValidationData(TreasureNodeId, "TreasureHunt", 2, 150, "CODE-123")
    ];

    public static IReadOnlyList<AssignedMissionData> AssignedTo(Guid operatorId, Guid missionId) =>
        [new AssignedMissionData(missionId, operatorId, "Mission")];
}
