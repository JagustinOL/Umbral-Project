using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.StartLiveSession;

public sealed class StartLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath_StartsSessionAndPersists()
    {
        // Arrange
        var repositoryMock = new Mock<ILiveSessionRepository>();
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var missionIntegrationMock = new Mock<IMissionIntegrationService>();
        var operatorId = Guid.NewGuid();
        var session = BuildPendingSession(operatorId);

        repositoryMock
            .Setup(x => x.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        missionIntegrationMock
            .Setup(x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AssignedMissionData(session.MissionRef, operatorId, "Mission")
            ]);

        teamRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new StartLiveSessionHandler(
            repositoryMock.Object,
            teamRepositoryMock.Object,
            missionIntegrationMock.Object);
        var command = new StartLiveSessionCommand(operatorId, session.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        session.Status.Should().Be(LiveSessionStatus.Active);
        repositoryMock.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutTeams_ThrowsConflictException()
    {
        // Arrange
        var repositoryMock = new Mock<ILiveSessionRepository>();
        var teamRepositoryMock = new Mock<ITeamRepository>();
        var missionIntegrationMock = new Mock<IMissionIntegrationService>();
        var operatorId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionRef: Guid.NewGuid(),
            operatorRef: operatorId,
            allowedNodes:
            [
                new AllowedNode(Guid.NewGuid(), "Trivia", 100)
            ],
            difficultyMultiplier: 1.0m);

        repositoryMock
            .Setup(x => x.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        missionIntegrationMock
            .Setup(x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AssignedMissionData(session.MissionRef, operatorId, "Mission")
            ]);

        var handler = new StartLiveSessionHandler(
            repositoryMock.Object,
            teamRepositoryMock.Object,
            missionIntegrationMock.Object);
        var command = new StartLiveSessionCommand(operatorId, session.Id);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        repositoryMock.Verify(x => x.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static LiveSession BuildPendingSession(Guid operatorId)
    {
        var session = LiveSession.CreateForMission(
            missionRef: Guid.NewGuid(),
            operatorRef: operatorId,
            allowedNodes:
            [
                new AllowedNode(Guid.NewGuid(), "Trivia", 100)
            ],
            difficultyMultiplier: 1.0m);

        session.RegisterTeam(Guid.NewGuid());
        return session;
    }
}

