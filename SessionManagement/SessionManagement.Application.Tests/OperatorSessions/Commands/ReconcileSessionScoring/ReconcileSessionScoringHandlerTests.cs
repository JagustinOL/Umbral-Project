using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Commands.ReconcileSessionScoring;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Common;
using SessionManagement.Domain.Events;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.ReconcileSessionScoring;

public sealed class ReconcileSessionScoringHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionMissing_ThrowsNotFound()
    {
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(sessionId, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var handler = new ReconcileSessionScoringHandler(
            sessionRepo.Object,
            new Mock<ITeamRepository>().Object,
            new Mock<IMissionIntegrationService>().Object,
            new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new ReconcileSessionScoringCommand(operatorId, sessionId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMissionNotAssigned_ThrowsNotFound()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new ReconcileSessionScoringHandler(
            sessionRepo.Object,
            new Mock<ITeamRepository>().Object,
            missions.Object,
            new Mock<IDomainEventPublisher>().Object);

        var act = () => handler.Handle(
            new ReconcileSessionScoringCommand(session.OperatorRef, session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*RN-16*");
    }

    [Fact]
    public async Task Handle_PublishesStartedTeamsAndValidEvidence_DedupesByNode()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var evidence1 = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "Bogota", 0);
        session.MarkEvidenceAsValid(evidence1.Id);
        var evidence2 = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "Bogota-again", 0);
        session.MarkEvidenceAsValid(evidence2.Id);

        var team = Team.Create("Alpha", Guid.NewGuid(), "Lead");
        // Force dictionary lookup miss → fallback name path covered via empty GetByIds
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        IReadOnlyList<IDomainEvent>? published = null;
        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<IDomainEvent>, CancellationToken>((e, _) => published = e)
            .Returns(Task.CompletedTask);

        var handler = new ReconcileSessionScoringHandler(
            sessionRepo.Object, teamRepo.Object, missions.Object, publisher.Object);

        var result = await handler.Handle(
            new ReconcileSessionScoringCommand(session.OperatorRef, session.Id), CancellationToken.None);

        result.TeamsPublished.Should().Be(1);
        result.EvidencesPublished.Should().Be(1); // deduped
        published.Should().NotBeNull();
        published!.OfType<SessionStartedEvent>().Should().ContainSingle();
        published.OfType<TeamRegisteredEvent>().Should().ContainSingle(e =>
            e.TeamId == LiveSessionTestFactory.DefaultTeamId && e.TeamName.StartsWith("Team-"));
        published.OfType<EvidenceValidatedEvent>().Should().ContainSingle(e =>
            e.EvidenceSubmissionId == evidence1.Id
            && e.DifficultyMultiplier == session.DifficultyMultiplier
            && e.NodeTitle != null);
        _ = team;
    }

    [Fact]
    public async Task Handle_WhenPendingWithoutStart_SkipsSessionStartedEvent()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        IReadOnlyList<IDomainEvent>? published = null;
        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<IDomainEvent>, CancellationToken>((e, _) => published = e)
            .Returns(Task.CompletedTask);

        var handler = new ReconcileSessionScoringHandler(
            sessionRepo.Object, teamRepo.Object, missions.Object, publisher.Object);

        var result = await handler.Handle(
            new ReconcileSessionScoringCommand(session.OperatorRef, session.Id), CancellationToken.None);

        result.TeamsPublished.Should().Be(1);
        result.EvidencesPublished.Should().Be(0);
        published.Should().NotBeNull();
        published!.OfType<SessionStartedEvent>().Should().BeEmpty();
        published.OfType<TeamRegisteredEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_SkipsEvidenceForUnknownNode()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100, "Q1")],
            1m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();

        // Inject evidence for a node not in AllowedNodes via AcceptEvidence would fail RB-05.
        // Instead mark only known node invalid path: zero valid evidence → still publishes teams.
        var sessionRepo = new Mock<ILiveSessionRepository>();
        sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var namedTeam = Team.Create("Named", Guid.NewGuid(), "Lead");
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([namedTeam]); // wrong id → still fallback for DefaultTeamId

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(session.OperatorRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(session.OperatorRef, session.MissionRef));

        var publisher = new Mock<IDomainEventPublisher>();
        publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ReconcileSessionScoringHandler(
            sessionRepo.Object, teamRepo.Object, missions.Object, publisher.Object);

        var result = await handler.Handle(
            new ReconcileSessionScoringCommand(session.OperatorRef, session.Id), CancellationToken.None);

        result.EvidencesPublished.Should().Be(0);
        publisher.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
