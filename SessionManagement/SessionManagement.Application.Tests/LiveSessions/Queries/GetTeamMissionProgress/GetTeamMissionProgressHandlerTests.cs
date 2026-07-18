using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.LiveSessions.Queries.GetTeamMissionProgress;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Queries.GetTeamMissionProgress;

public sealed class GetTeamMissionProgressHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOrderedNodesWithCompletion()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"]),
            new NodeValidationRule(LiveSessionTestFactory.TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];
        session.SubmitTriviaAnswer(
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TriviaNodeId,
            "Bogota",
            0,
            rules);

        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        integration.Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());

        var handler = new GetTeamMissionProgressHandler(repo.Object, integration.Object);
        var result = await handler.Handle(
            new GetTeamMissionProgressQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        result.TotalNodes.Should().Be(2);
        result.CompletedNodes.Should().Be(1);
        result.IsMissionCompleted.Should().BeFalse();
        result.Nodes[0].NodeId.Should().Be(LiveSessionTestFactory.TriviaNodeId);
        result.Nodes[0].IsCompleted.Should().BeTrue();
        result.Nodes[1].IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAllCompleted_MarksMissionCompleted()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"]),
            new NodeValidationRule(LiveSessionTestFactory.TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];
        session.SubmitTriviaAnswer(
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TriviaNodeId,
            "Bogota",
            0,
            rules);
        session.SubmitTreasureHuntCode(
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TreasureNodeId,
            "CODE-123",
            rules);

        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        integration.Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());

        var handler = new GetTeamMissionProgressHandler(repo.Object, integration.Object);
        var result = await handler.Handle(
            new GetTeamMissionProgressQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        result.IsMissionCompleted.Should().BeTrue();
        result.CompletedNodes.Should().Be(2);
        result.Nodes.Should().OnlyContain(n => n.IsCompleted);
    }

    [Fact]
    public async Task Handle_WhenSessionMissing_ThrowsNotFound()
    {
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Aggregates.LiveSession?)null);

        var handler = new GetTeamMissionProgressHandler(repo.Object, integration.Object);
        var act = () => handler.Handle(
            new GetTeamMissionProgressQuery(Guid.NewGuid(), LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
