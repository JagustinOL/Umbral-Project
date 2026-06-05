using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.CancelLiveSession;

public sealed class CancelLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<ILiveSessionRepository>();
        var teams = new Mock<ITeamRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdForOperatorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var handler = new CancelLiveSessionHandler(repo.Object, teams.Object, integration.Object);
        var act = () => handler.Handle(new CancelLiveSessionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None); // OperatorId, SessionId

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_CancelsSession()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var repo = new Mock<ILiveSessionRepository>();
        var teams = new Mock<ITeamRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));
        teams.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Team>());

        var handler = new CancelLiveSessionHandler(repo.Object, teams.Object, integration.Object);
        await handler.Handle(new CancelLiveSessionCommand(session.OperatorRef, session.Id), CancellationToken.None);

        session.Status.Should().Be(LiveSessionStatus.Cancelled);
        repo.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }
}
