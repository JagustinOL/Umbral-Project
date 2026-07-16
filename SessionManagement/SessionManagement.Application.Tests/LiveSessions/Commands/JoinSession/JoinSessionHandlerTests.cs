using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.LiveSessions.Commands.JoinSession;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Commands.JoinSession;

public sealed class JoinSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenTeamIsLocked_ThrowsConflictBeforeRegisteringSession()
    {
        // Arrange
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

        var eventPublisher = new Mock<IDomainEventPublisher>();
        var handler = new JoinSessionHandler(sessionRepository.Object, teamRepository.Object, eventPublisher.Object);

        // Act
        var act = () => handler.Handle(
            new JoinSessionCommand(session.JoinCode, team.Id),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*RN-13*");

        sessionRepository.Verify(
            r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesPendingJoinRequest()
    {
        // Arrange
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(Guid.NewGuid(), "Trivia", 10)], 1m);
        var team = Team.Create("Squad", Guid.NewGuid(), "Leader");

        var sessionRepository = new Mock<ILiveSessionRepository>();
        sessionRepository.Setup(r => r.GetByJoinCodeAsync(session.JoinCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var teamRepository = new Mock<ITeamRepository>();
        teamRepository.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var eventPublisher = new Mock<IDomainEventPublisher>();
        var handler = new JoinSessionHandler(sessionRepository.Object, teamRepository.Object, eventPublisher.Object);

        // Act
        var result = await handler.Handle(new JoinSessionCommand(session.JoinCode, team.Id), CancellationToken.None);

        // Assert
        result.SessionId.Should().Be(session.Id);
        result.Status.Should().Be(JoinRequestStatus.Pending.ToString());
        result.RequestId.Should().NotBeNull();
        session.RegisteredTeamIds.Should().NotContain(team.Id);
        session.JoinRequests.Should().ContainSingle(x => x.TeamId == team.Id && x.Status == JoinRequestStatus.Pending);
        sessionRepository.Verify(r => r.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        teamRepository.Verify(r => r.SaveAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJoinCodeInvalid_ThrowsNotFoundException()
    {
        // Arrange
        var sessionRepository = new Mock<ILiveSessionRepository>();
        sessionRepository.Setup(r => r.GetByJoinCodeAsync("BAD", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);
        var handler = new JoinSessionHandler(sessionRepository.Object, new Mock<ITeamRepository>().Object, new Mock<IDomainEventPublisher>().Object);

        // Act
        var act = () => handler.Handle(new JoinSessionCommand("BAD", Guid.NewGuid()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTeamAlreadyRegistered_ReturnsApprovedIdempotently()
    {
        // Arrange
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

        var eventPublisher = new Mock<IDomainEventPublisher>();
        var handler = new JoinSessionHandler(sessionRepository.Object, teamRepository.Object, eventPublisher.Object);

        // Act
        var result = await handler.Handle(new JoinSessionCommand(session.JoinCode, team.Id), CancellationToken.None);

        // Assert
        result.SessionId.Should().Be(session.Id);
        result.Status.Should().Be(JoinRequestStatus.Approved.ToString());
        sessionRepository.Verify(r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
