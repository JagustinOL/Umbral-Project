using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Evidence.Processing;
using SessionManagement.Application.Evidence.Validation;
using SessionManagement.Application.Evidence.Validation.Handlers;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Facades;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Common;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.Facades;

public sealed class SessionOperationFacadeTests
{
    private readonly Mock<ILiveSessionRepository> _sessionRepo = new();
    private readonly Mock<ITeamRepository> _teamRepo = new();
    private readonly Mock<IMissionIntegrationService> _missions = new();
    private readonly Mock<IDomainEventPublisher> _publisher = new();
    private readonly Mock<ILiveSessionRealtimeNotifier> _notifier = new();

    private SessionOperationFacade CreateFacade()
    {
        var validator = new EvidenceValidatorService(
            new SessionActiveValidationHandler(),
            new TeamRegisteredValidationHandler(),
            new NodeAllowedValidationHandler(),
            new SequentialProgressValidationHandler(),
            new AnswerCorrectnessValidationHandler());

        return new SessionOperationFacade(
            _sessionRepo.Object,
            _teamRepo.Object,
            _missions.Object,
            _publisher.Object,
            _notifier.Object,
            new TriviaEvidenceSubmissionProcessor(validator),
            new TreasureHuntEvidenceSubmissionProcessor(validator));
    }

    private void SetupTeamLock(LiveSession session)
    {
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        _teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);
        _teamRepo.Setup(r => r.SaveRangeAsync(It.IsAny<IReadOnlyList<Team>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _ = session;
    }

    [Fact]
    public async Task CreateSessionAsync_WhenMissionAssignedAndActive_CreatesSession()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, missionId));
        _missions.Setup(m => m.GetMissionStatusAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Active");
        _missions.Setup(m => m.GetNodeValidationDataAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());
        _missions.Setup(m => m.GetMissionDifficultyMultiplierAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.5m);
        _sessionRepo.Setup(r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateFacade().CreateSessionAsync(operatorId, missionId);

        result.SessionId.Should().NotBe(Guid.Empty);
        result.JoinCode.Should().NotBeNullOrWhiteSpace();
        _sessionRepo.Verify(r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_WhenMissionNotAssigned_ThrowsNotFound()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var act = () => CreateFacade().CreateSessionAsync(operatorId, missionId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateSessionAsync_WhenMissionNotActive_ThrowsConflict()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, missionId));
        _missions.Setup(m => m.GetMissionStatusAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Draft");

        var act = () => CreateFacade().CreateSessionAsync(operatorId, missionId);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task StartSessionAsync_WhenPendingWithTeam_StartsAndLocksTeams()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var operatorId = session.OperatorRef;
        SetupTeamLock(session);

        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, session.MissionRef));
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateFacade().StartSessionAsync(operatorId, session.Id);

        session.Status.Should().Be(LiveSessionStatus.Active);
        _teamRepo.Verify(r => r.SaveRangeAsync(It.IsAny<IReadOnlyList<Team>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartSessionAsync_WhenSessionNotFound_ThrowsNotFound()
    {
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(sessionId, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var act = () => CreateFacade().StartSessionAsync(operatorId, sessionId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task StartSessionAsync_WhenNoTeams_ThrowsConflict()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionId,
            operatorId,
            [new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100)],
            1m);

        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, missionId));

        var act = () => CreateFacade().StartSessionAsync(operatorId, session.Id);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task FinalizeSessionAsync_WhenActive_FinalizesAndReleasesTeams()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var operatorId = session.OperatorRef;
        SetupTeamLock(session);
        // Teams may already be unlocked; Assign then Lock path not needed for release
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        team.AssignToSession(session.Id);
        team.Lock();
        _teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);

        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, session.MissionRef));
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateFacade().FinalizeSessionAsync(operatorId, session.Id);

        session.Status.Should().Be(LiveSessionStatus.Finalized);
        team.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task FinalizeSessionAsync_WhenAlreadyFinalized_IsIdempotent()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        session.Finalize();
        var operatorId = session.OperatorRef;
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        team.AssignToSession(session.Id);
        team.Lock();
        _teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);

        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, session.MissionRef));

        await CreateFacade().FinalizeSessionAsync(operatorId, session.Id);

        session.Status.Should().Be(LiveSessionStatus.Finalized);
        _sessionRepo.Verify(r => r.SaveAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Never);
        team.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task CancelSessionAsync_WhenPending_CancelsAndReleases()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var operatorId = session.OperatorRef;
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        team.AssignToSession(session.Id);
        team.Lock();
        _teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);

        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, session.MissionRef));
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateFacade().CancelSessionAsync(operatorId, session.Id);

        session.Status.Should().Be(LiveSessionStatus.Cancelled);
        team.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task CancelSessionAsync_WhenActive_CancelsAndReleases()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var operatorId = session.OperatorRef;
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        team.AssignToSession(session.Id);
        team.Lock();
        _teamRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);

        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, session.MissionRef));
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateFacade().CancelSessionAsync(operatorId, session.Id);

        session.Status.Should().Be(LiveSessionStatus.Cancelled);
        team.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitTriviaAsync_WhenCorrect_ReturnsResultAndNotifies()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier.Setup(n => n.NotifyTriviaAnswerSubmittedAsync(
                session.Id, LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId,
                true, true, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier.Setup(n => n.NotifyTeamProgressUpdatedAsync(
                session.Id, LiveSessionTestFactory.DefaultTeamId, It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateFacade().SubmitTriviaAsync(
            session.Id,
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TriviaNodeId,
            "Bogota",
            0);

        result.IsCorrect.Should().BeTrue();
        result.NodeCompleted.Should().BeTrue();
        result.AwardedPoints.Should().Be(100);
        _notifier.Verify(n => n.NotifyTriviaAnswerSubmittedAsync(
            session.Id, LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId,
            true, true, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitTriviaAsync_WhenSessionMissing_ThrowsNotFound()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var act = () => CreateFacade().SubmitTriviaAsync(
            sessionId, Guid.NewGuid(), Guid.NewGuid(), "x", 0);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitTreasureHuntAsync_WhenCorrect_ReleasesHintsAndNotifies()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 150, "Hunt")],
            1m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();

        var hintId = Guid.NewGuid();
        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(
                    LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 1, 150, ["CODE-123"], "Hunt")
            ]);
        _missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TreasureNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MissionHintData(hintId, 1, "Mira atrás", 5)]);
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier.Setup(n => n.NotifyHuntLocationReachedAsync(
                session.Id, LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TreasureNodeId,
                true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier.Setup(n => n.NotifyTeamProgressUpdatedAsync(
                session.Id, LiveSessionTestFactory.DefaultTeamId, It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateFacade().SubmitTreasureHuntAsync(
            session.Id,
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TreasureNodeId,
            "CODE-123");

        result.IsCorrect.Should().BeTrue();
        result.NodeCompleted.Should().BeTrue();
        session.ReleasedHints.Should().Contain(h => h.HintId == hintId);
    }

    [Fact]
    public async Task SubmitTreasureHuntAsync_WhenNoCatalogHints_SkipsAutoRelease()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 150)],
            1m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();

        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(
                    LiveSessionTestFactory.TreasureNodeId, "Treasure_Hunt", 1, 150, ["CODE-123"])
            ]);
        _missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TreasureNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier.Setup(n => n.NotifyHuntLocationReachedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notifier.Setup(n => n.NotifyTeamProgressUpdatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateFacade().SubmitTreasureHuntAsync(
            session.Id,
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TreasureNodeId,
            "code-123");

        result.IsCorrect.Should().BeTrue();
        session.ReleasedHints.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAndPublishAsync_WhenNoEvents_OnlySaves()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        session.ClearDomainEvents();
        _sessionRepo.Setup(r => r.SaveAsync(session, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await CreateFacade().SaveAndPublishAsync(session);

        _sessionRepo.Verify(r => r.SaveAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<IReadOnlyList<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartSessionAsync_WhenMissionNotAssigned_ThrowsNotFound()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var operatorId = session.OperatorRef;
        _sessionRepo.Setup(r => r.GetByIdForOperatorAsync(session.Id, operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _missions.Setup(m => m.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var act = () => CreateFacade().StartSessionAsync(operatorId, session.Id);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*no está asignada al operador*");
    }

    [Fact]
    public async Task SubmitTriviaAsync_WhenUnsupportedNodeType_Throws()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        _sessionRepo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(LiveSessionTestFactory.TriviaNodeId, "Unknown", 1, 100, ["x"])
            ]);

        var act = () => CreateFacade().SubmitTriviaAsync(
            session.Id,
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TriviaNodeId,
            "x",
            0);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no soportado*");
    }
}
