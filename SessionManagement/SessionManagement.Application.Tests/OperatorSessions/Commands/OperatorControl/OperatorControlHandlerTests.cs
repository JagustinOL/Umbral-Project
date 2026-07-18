using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Commands.ApplyManualPenalty;
using SessionManagement.Application.OperatorSessions.Commands.ReleaseManualHint;
using SessionManagement.Application.OperatorSessions.Commands.SendSupportMessage;
using SessionManagement.Application.OperatorSessions.Commands.ToggleSessionPause;
using SessionManagement.Application.OperatorSessions.Queries.GetSessionJoinRequests;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Common;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.OperatorControl;

public sealed class OperatorControlHandlerTests
{
    private static readonly Guid HintId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task ReleaseManualHint_HappyPath_SavesAndPublishes()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());
        missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MissionHintData(HintId, 1, "Pista", 5)]);

        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ReleaseManualHintHandler(sessionRepo.Object, missions.Object, publisher.Object);

        await handler.Handle(
            new ReleaseManualHintCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, HintId),
            CancellationToken.None);

        session.ReleasedHints.Should().Contain(h => h.HintId == HintId && h.WasManualRelease);
        publisher.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseManualHint_WhenHintMissing_ThrowsNotFound()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());
        missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new ReleaseManualHintHandler(
            sessionRepo.Object, missions.Object, new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new ReleaseManualHintCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, HintId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ReleaseManualHint_WhenTeamCompleted_ThrowsConflict()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100)], 1m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();
        var evidence = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "ok", 0);
        session.MarkEvidenceAsValid(evidence.Id);
        session.MarkTeamCompleted(LiveSessionTestFactory.DefaultTeamId);

        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(LiveSessionTestFactory.TriviaNodeId, "Trivia", 1, 100, ["ok"])
            ]);

        var handler = new ReleaseManualHintHandler(
            sessionRepo.Object, missions.Object, new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new ReleaseManualHintCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, HintId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ApplyManualPenalty_HappyPath_Publishes()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ApplyManualPenaltyHandler(sessionRepo.Object, missions.Object, publisher.Object);

        await handler.Handle(
            new ApplyManualPenaltyCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, 10, "Trampa"),
            CancellationToken.None);

        publisher.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyManualPenalty_WhenPointsInvalid_ThrowsConflict()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var handler = new ApplyManualPenaltyHandler(
            sessionRepo.Object, missions.Object, new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new ApplyManualPenaltyCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, 0, "x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ToggleSessionPause_WhenActive_Pauses()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ToggleSessionPauseHandler(sessionRepo.Object, missions.Object, publisher.Object);

        var status = await handler.Handle(
            new ToggleSessionPauseCommand(session.OperatorRef, session.Id, "Break"),
            CancellationToken.None);

        status.Should().Be(nameof(LiveSessionStatus.Paused));
    }

    [Fact]
    public async Task ToggleSessionPause_WhenPending_ThrowsConflict()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var handler = new ToggleSessionPauseHandler(
            sessionRepo.Object, missions.Object, new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new ToggleSessionPauseCommand(session.OperatorRef, session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SendSupportMessage_HappyPath_Publishes()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new SendSupportMessageHandler(sessionRepo.Object, missions.Object, publisher.Object);

        await handler.Handle(
            new SendSupportMessageCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, "Ánimo"),
            CancellationToken.None);

        publisher.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendSupportMessage_WhenEmpty_ThrowsConflict()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var handler = new SendSupportMessageHandler(
            sessionRepo.Object, missions.Object, new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new SendSupportMessageCommand(
                session.OperatorRef, session.Id, LiveSessionTestFactory.DefaultTeamId, "  "),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task GetSessionJoinRequests_ReturnsOrderedDtos()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionId, operatorId,
            [new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100)], 1m);

        var team = Team.Create("Los Exploradores", Guid.NewGuid(), "Ana");
        session.SubmitJoinRequest(team.Id, session.JoinCode);

        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, missionId));

        var handler = new GetSessionJoinRequestsHandler(sessionRepo.Object, teamRepo.Object, missions.Object);

        var result = await handler.Handle(
            new GetSessionJoinRequestsQuery(operatorId, session.Id), CancellationToken.None);

        result.Should().ContainSingle(r =>
            r.TeamId == team.Id
            && r.TeamName == "Los Exploradores"
            && r.Status == "Pending");
    }

    [Fact]
    public async Task GetSessionJoinRequests_WhenSessionMissing_ThrowsNotFound()
    {
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var handler = new GetSessionJoinRequestsHandler(
            sessionRepo.Object,
            new Mock<ITeamRepository>().Object,
            new Mock<IMissionIntegrationService>().Object);

        var act = () => handler.Handle(
            new GetSessionJoinRequestsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
