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

    [Fact]
    public async Task Handle_WhenValid_JoinsTeamToSession()
    {
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(Guid.NewGuid(), "Trivia", 10)], 1m);
        var team = Team.Create("Squad", Guid.NewGuid(), "Leader");

        var sessionRepository = new Mock<ILiveSessionRepository>();
        sessionRepository.Setup(r => r.GetByJoinCodeAsync(session.JoinCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var teamRepository = new Mock<ITeamRepository>();
        teamRepository.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new JoinSessionHandler(sessionRepository.Object, teamRepository.Object);
        var result = await handler.Handle(new JoinSessionCommand(session.JoinCode, team.Id), CancellationToken.None);

        result.Should().Be(session.Id);
        session.RegisteredTeamIds.Should().Contain(team.Id);
        sessionRepository.Verify(r => r.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        teamRepository.Verify(r => r.SaveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenJoinCodeInvalid_ThrowsNotFoundException()
    {
        var sessionRepository = new Mock<ILiveSessionRepository>();
        sessionRepository.Setup(r => r.GetByJoinCodeAsync("BAD", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);
        var handler = new JoinSessionHandler(sessionRepository.Object, new Mock<ITeamRepository>().Object);

        var act = () => handler.Handle(new JoinSessionCommand("BAD", Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTeamAlreadyInSession_ReturnsSessionIdIdempotently()
    {
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(Guid.NewGuid(), "Trivia", 10)], 1m);
        var team = Team.Create("Squad", Guid.NewGuid(), "Leader");
        session.RegisterTeam(team.Id);
        team.AssignToSession(session.Id);

        var sessionRepository = new Mock<ILiveSessionRepository>();
        sessionRepository.Setup(r => r.GetByJoinCodeAsync(session.JoinCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var teamRepository = new Mock<ITeamRepository>();
        teamRepository.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new JoinSessionHandler(sessionRepository.Object, teamRepository.Object);
        var result = await handler.Handle(new JoinSessionCommand(session.JoinCode, team.Id), CancellationToken.None);

        result.Should().Be(session.Id);
        sessionRepository.Verify(r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
