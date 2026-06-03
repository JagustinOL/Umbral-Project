using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Commands.SubmitTriviaAnswer;

public sealed class SubmitTriviaAnswerHandlerTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TriviaNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TreasureNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenAnswerMatchesExpectedValue()
    {
        // Arrange
        var repositoryMock = new Mock<ILiveSessionRepository>();
        var missionIntegrationMock = new Mock<IMissionIntegrationService>();
        var session = BuildStartedSession();

        repositoryMock
            .Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        missionIntegrationMock
            .Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(TriviaNodeId, "Trivia", 1, 100, "Bogota"),
                new MissionNodeValidationData(TreasureNodeId, "TreasureHunt", 2, 150, "CODE-123")
            ]);

        var handler = new SubmitTriviaAnswerHandler(repositoryMock.Object, missionIntegrationMock.Object);
        var command = new SubmitTriviaAnswerCommand(session.Id, TeamId, TriviaNodeId, "Bogota");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeTrue();
        result.NextNodeId.Should().Be(TreasureNodeId);
        repositoryMock.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAnswerDoesNotMatchExpectedValue()
    {
        // Arrange
        var repositoryMock = new Mock<ILiveSessionRepository>();
        var missionIntegrationMock = new Mock<IMissionIntegrationService>();
        var session = BuildStartedSession();

        repositoryMock
            .Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        missionIntegrationMock
            .Setup(x => x.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(TriviaNodeId, "Trivia", 1, 100, "Bogota"),
                new MissionNodeValidationData(TreasureNodeId, "TreasureHunt", 2, 150, "CODE-123")
            ]);

        var handler = new SubmitTriviaAnswerHandler(repositoryMock.Object, missionIntegrationMock.Object);
        var command = new SubmitTriviaAnswerCommand(session.Id, TeamId, TriviaNodeId, "Medellin");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeFalse();
        result.NextNodeId.Should().Be(TriviaNodeId);
        repositoryMock.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static LiveSession BuildStartedSession()
    {
        var session = LiveSession.Create(
            missionRef: Guid.NewGuid(),
            operatorRef: Guid.NewGuid(),
            allowedNodes:
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            difficultyMultiplier: 1.0m);

        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        return session;
    }
}

