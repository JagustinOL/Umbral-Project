using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.FinalizeLiveSession;

public sealed class FinalizeLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_FinalizesSession()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        var teams = new Mock<ITeamRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        repo.Setup(x => x.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));
        teams.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Team>());

        var handler = new FinalizeLiveSessionHandler(repo.Object, teams.Object, integration.Object);
        await handler.Handle(new FinalizeLiveSessionCommand(session.OperatorRef, session.Id), CancellationToken.None);

        session.Status.Should().Be(LiveSessionStatus.Finalized);
        repo.Verify(x => x.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }
}
