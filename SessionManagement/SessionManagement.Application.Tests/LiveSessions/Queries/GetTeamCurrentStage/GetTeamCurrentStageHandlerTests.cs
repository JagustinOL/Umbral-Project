using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentStage;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Queries.GetTeamCurrentStage;

public sealed class GetTeamCurrentStageHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCurrentStage()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        integration.Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());

        var handler = new GetTeamCurrentStageHandler(repo.Object, integration.Object);
        var result = await handler.Handle(
            new GetTeamCurrentStageQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        result.CurrentNodeId.Should().Be(LiveSessionTestFactory.TriviaNodeId);
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAllNodesCompleted_ReturnsCompletedDto()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, "Bogota"),
            new NodeValidationRule(LiveSessionTestFactory.TreasureNodeId, 2, NodeValidationType.TreasureHunt, "CODE-123")
        ];
        session.SubmitTriviaAnswer(LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "Bogota", rules);
        session.SubmitTreasureHuntCode(LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TreasureNodeId, "CODE-123", rules);

        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        integration.Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());

        var handler = new GetTeamCurrentStageHandler(repo.Object, integration.Object);
        var result = await handler.Handle(
            new GetTeamCurrentStageQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        result.IsCompleted.Should().BeTrue();
        result.CurrentNodeId.Should().BeNull();
    }
}
