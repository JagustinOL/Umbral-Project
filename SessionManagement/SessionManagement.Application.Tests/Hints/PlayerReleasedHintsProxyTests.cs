using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Hints;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.Hints;

public sealed class PlayerReleasedHintsProxyTests
{
    private static readonly Guid HintId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task GetReleasedHintsForTeamAsync_WhenSessionMissing_ThrowsNotFound()
    {
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var proxy = new PlayerReleasedHintsProxy(repo.Object, new Mock<IMissionIntegrationService>().Object);

        var act = () => proxy.GetReleasedHintsForTeamAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetReleasedHintsForTeamAsync_WhenTeamNotRegistered_ThrowsNotFound()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var proxy = new PlayerReleasedHintsProxy(repo.Object, new Mock<IMissionIntegrationService>().Object);

        var act = () => proxy.GetReleasedHintsForTeamAsync(session.Id, Guid.NewGuid(), LiveSessionTestFactory.TriviaNodeId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetReleasedHintsForTeamAsync_ReturnsOnlyReleasedCatalogHints()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var rules = new[]
        {
            new NodeValidationRule(LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"])
        };
        session.ReleaseHint(
            LiveSessionTestFactory.DefaultTeamId,
            HintId,
            LiveSessionTestFactory.TriviaNodeId,
            5,
            rules,
            wasManualRelease: true,
            hintOrder: 1);

        var otherHint = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionHintData(HintId, 1, "Pista A", 5),
                new MissionHintData(otherHint, 2, "Pista B", 3)
            ]);

        var proxy = new PlayerReleasedHintsProxy(repo.Object, missions.Object);

        var result = await proxy.GetReleasedHintsForTeamAsync(
            session.Id,
            LiveSessionTestFactory.DefaultTeamId,
            LiveSessionTestFactory.TriviaNodeId);

        result.Should().ContainSingle(h => h.Id == HintId && h.Content == "Pista A" && h.Order == 1);
    }

    [Fact]
    public async Task GetAllReleasedHintsForTeamAsync_WhenNone_ReturnsEmpty()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var proxy = new PlayerReleasedHintsProxy(repo.Object, new Mock<IMissionIntegrationService>().Object);

        var result = await proxy.GetAllReleasedHintsForTeamAsync(
            session.Id, LiveSessionTestFactory.DefaultTeamId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllReleasedHintsForTeamAsync_SkipsHintsMissingFromCatalog_AndLoadsContext()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var rules = new[]
        {
            new NodeValidationRule(LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"])
        };
        session.ReleaseHint(
            LiveSessionTestFactory.DefaultTeamId,
            HintId,
            LiveSessionTestFactory.TriviaNodeId,
            5,
            rules,
            wasManualRelease: true,
            hintOrder: 1);
        var orphanHint = Guid.NewGuid();
        session.ReleaseHint(
            LiveSessionTestFactory.DefaultTeamId,
            orphanHint,
            LiveSessionTestFactory.TriviaNodeId,
            2,
            rules,
            wasManualRelease: false,
            hintOrder: 2);

        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MissionHintData(HintId, 1, "Pista A", 5)]);
        missions.Setup(m => m.GetNodePlayerContentAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerNodeContentData(
                LiveSessionTestFactory.TriviaNodeId,
                "Trivia",
                [new PlayerTriviaQuestionData("¿Capital?", ["A", "B"])],
                null,
                null));

        var proxy = new PlayerReleasedHintsProxy(repo.Object, missions.Object);

        var result = await proxy.GetAllReleasedHintsForTeamAsync(
            session.Id, LiveSessionTestFactory.DefaultTeamId);

        result.Should().ContainSingle(h =>
            h.HintId == HintId
            && h.WasManualRelease
            && h.NodeType == "Trivia"
            && h.NodePrompt!.Contains("Capital"));
    }

    [Fact]
    public async Task GetAllReleasedHintsForTeamAsync_WhenContextFails_ContinuesWithNullPrompt()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var rules = new[]
        {
            new NodeValidationRule(LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"])
        };
        session.ReleaseHint(
            LiveSessionTestFactory.DefaultTeamId,
            HintId,
            LiveSessionTestFactory.TriviaNodeId,
            5,
            rules);

        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetHintsForNodeAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MissionHintData(HintId, 1, "Pista A", 5)]);
        missions.Setup(m => m.GetNodePlayerContentAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var proxy = new PlayerReleasedHintsProxy(repo.Object, missions.Object);

        var result = await proxy.GetAllReleasedHintsForTeamAsync(
            session.Id, LiveSessionTestFactory.DefaultTeamId);

        result.Should().ContainSingle(h => h.HintId == HintId && h.NodeType == null && h.NodePrompt == null);
    }
}
