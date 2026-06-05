using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Commands.SubmitTreasureHuntCode;

public sealed class SubmitTreasureHuntCodeHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SessionManagement.Domain.Aggregates.LiveSession?)null);

        var handler = new SubmitTreasureHuntCodeHandler(repo.Object, integration.Object);
        var act = () => handler.Handle(
            new SubmitTreasureHuntCodeCommand(Guid.NewGuid(), LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TreasureNodeId, "X"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCodeCorrect_ReturnsSuccess()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[] { new AllowedNode(LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 150) },
            1.0m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        integration.Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new MissionNodeValidationData(LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 1, 150, "CODE-123") });

        var handler = new SubmitTreasureHuntCodeHandler(repo.Object, integration.Object);
        var result = await handler.Handle(
            new SubmitTreasureHuntCodeCommand(session.Id, LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TreasureNodeId, "CODE-123"),
            CancellationToken.None);

        result.IsCorrect.Should().BeTrue();
        repo.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }
}
