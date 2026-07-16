using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Queries.GetSessionTeams;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Queries.GetSessionTeams;

public sealed class GetSessionTeamsHandlerTests
{
    [Fact]
    public async Task Handle_WhenActive_ReturnsTeamIds()
    {
        // Arrange
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var handler = new GetSessionTeamsHandler(repo.Object, integration.Object);

        // Act
        var result = await handler.Handle(new GetSessionTeamsQuery(session.OperatorRef, session.Id), CancellationToken.None);

        // Assert
        result.TeamIds.Should().Contain(LiveSessionTestFactory.DefaultTeamId);
    }

    [Fact]
    public async Task Handle_WhenFinalized_ThrowsConflictException()
    {
        // Arrange
        var session = LiveSessionTestFactory.BuildActiveSession();
        session.Finalize();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var handler = new GetSessionTeamsHandler(repo.Object, integration.Object);

        // Act
        var act = () => handler.Handle(new GetSessionTeamsQuery(session.OperatorRef, session.Id), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenPending_ReturnsTeamIds()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var handler = new GetSessionTeamsHandler(repo.Object, integration.Object);
        var result = await handler.Handle(new GetSessionTeamsQuery(session.OperatorRef, session.Id), CancellationToken.None);

        result.TeamCount.Should().Be(1);
        result.TeamIds.Should().Contain(LiveSessionTestFactory.DefaultTeamId);
    }
}
