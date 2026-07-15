using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorSessionBoard;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Queries.GetOperatorSessionBoard;

public sealed class GetOperatorSessionBoardHandlerTests
{
    private static readonly Guid OperatorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MissionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid HintId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task Handle_ReturnsAvailableHintsForCurrentNode()
    {
        var session = LiveSession.Create(MissionId, OperatorId, [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdForOperatorAsync(session.Id, OperatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var mission = new Mock<IMissionIntegrationService>();
        mission.Setup(x => x.GetAssignedMissionsForOperatorAsync(OperatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AssignedMissionData(MissionId, OperatorId, "Test")]);
        mission.Setup(x => x.GetNodeValidationDataAsync(MissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MissionNodeValidationData(NodeId, "Trivia", 1, 100, ["ok"])]);
        mission.Setup(x => x.GetHintsForNodeAsync(MissionId, NodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MissionHintData(HintId, 1, "Busca detrás del marco", 5)]);
        mission.Setup(x => x.GetNodePlayerContentAsync(MissionId, NodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerNodeContentData(
                NodeId,
                "Trivia",
                [new PlayerTriviaQuestionData("¿Quién es el goleador?", ["A", "B"])],
                null,
                null));

        var handler = new GetOperatorSessionBoardHandler(sessionRepo.Object, teamRepo.Object, mission.Object);

        var result = await handler.Handle(
            new GetOperatorSessionBoardQuery(OperatorId, session.Id),
            CancellationToken.None);

        result.Teams.Should().ContainSingle();
        var team = result.Teams[0];
        team.CurrentNodeId.Should().Be(NodeId);
        team.CurrentGameLabel.Should().Contain("¿Quién es el goleador?");
        team.AvailableHints.Should().ContainSingle(h =>
            h.HintId == HintId
            && h.Content.Contains("marco")
            && h.NodePrompt == "¿Quién es el goleador?");
        team.IsMissionCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenMissionNotAssigned_ThrowsNotFound()
    {
        var session = LiveSession.Create(MissionId, OperatorId, [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdForOperatorAsync(session.Id, OperatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var mission = new Mock<IMissionIntegrationService>();
        mission.Setup(x => x.GetAssignedMissionsForOperatorAsync(OperatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetOperatorSessionBoardHandler(
            sessionRepo.Object, new Mock<ITeamRepository>().Object, mission.Object);

        var act = () => handler.Handle(
            new GetOperatorSessionBoardQuery(OperatorId, session.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
