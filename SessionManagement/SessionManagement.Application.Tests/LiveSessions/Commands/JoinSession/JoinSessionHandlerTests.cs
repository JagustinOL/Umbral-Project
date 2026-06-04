using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.LiveSessions.Commands.JoinSession;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Commands.JoinSession;

public sealed class JoinSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenTeamIsLocked_ThrowsConflictBeforeRegisteringSession()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionId,
            operatorId,
            [new AllowedNode(Guid.NewGuid(), "Trivia", 10)],
            1m);
        var team = Team.Create("Locked Squad", Guid.NewGuid(), "Leader");
        team.AssignToSession(Guid.NewGuid());
        team.Lock();

        var sessionRepository = new Mock<ILiveSessionRepository>();
        sessionRepository
            .Setup(r => r.GetByJoinCodeAsync(session.JoinCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var teamRepository = new Mock<ITeamRepository>();
        teamRepository
            .Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new JoinSessionHandler(sessionRepository.Object, teamRepository.Object);

        var act = () => handler.Handle(
            new JoinSessionCommand(session.JoinCode, team.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*RN-13*");

        sessionRepository.Verify(
            r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
