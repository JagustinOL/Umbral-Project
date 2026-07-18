using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.OperatorSessions.Commands.ProcessSessionJoinRequest;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.ProcessSessionJoinRequest;

public sealed class ProcessSessionJoinRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenApprove_RegistersTeamAndAssignsSession()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionId, operatorId, [new AllowedNode(Guid.NewGuid(), "Trivia", 100)], 1m);
        var team = Team.Create("Alpha", Guid.NewGuid(), "Leader");
        session.SubmitJoinRequest(team.Id, session.JoinCode);

        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AssignedMissionData(missionId, operatorId, "M1")]);
        var publisher = new Mock<IDomainEventPublisher>();

        var handler = new ProcessSessionJoinRequestHandler(
            sessionRepo.Object, teamRepo.Object, missions.Object, publisher.Object);

        // Act
        await handler.Handle(
            new ProcessSessionJoinRequestCommand(operatorId, session.Id, team.Id, Approve: true),
            CancellationToken.None);

        // Assert
        session.RegisteredTeamIds.Should().Contain(team.Id);
        team.CurrentSessionRef.Should().Be(session.Id);
        sessionRepo.Verify(r => r.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        teamRepo.Verify(r => r.SaveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReject_DoesNotRegisterTeam()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionId, operatorId, [new AllowedNode(Guid.NewGuid(), "Trivia", 100)], 1m);
        var team = Team.Create("Beta", Guid.NewGuid(), "Leader");
        session.SubmitJoinRequest(team.Id, session.JoinCode);

        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AssignedMissionData(missionId, operatorId, "M1")]);

        var handler = new ProcessSessionJoinRequestHandler(
            sessionRepo.Object, teamRepo.Object, missions.Object, new Mock<IDomainEventPublisher>().Object);

        // Act
        await handler.Handle(
            new ProcessSessionJoinRequestCommand(operatorId, session.Id, team.Id, Approve: false),
            CancellationToken.None);

        // Assert
        session.RegisteredTeamIds.Should().NotContain(team.Id);
        session.JoinRequests.Single().Status.Should().Be(JoinRequestStatus.Rejected);
        teamRepo.Verify(r => r.SaveAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
