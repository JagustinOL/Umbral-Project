using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionAdvancedTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly Guid NodeB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static LiveSession BuildActiveSession()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        return session;
    }

    private static IReadOnlyList<NodeValidationRule> SingleNodeRules() =>
        [new NodeValidationRule(NodeId, 1, NodeValidationType.Trivia, ["ok"])];

    [Fact]
    public void ReleaseHint_WhenActive_AddsReleasedHint()
    {
        var session = BuildActiveSession();
        var hintId = Guid.NewGuid();

        session.ReleaseHint(TeamId, hintId, NodeId, penaltyPoints: 10, SingleNodeRules());

        session.ReleasedHints.Should().ContainSingle(r => r.HintId == hintId);
        session.DomainEvents.Should().Contain(e => e.GetType().Name == "HintReleasedEvent");
    }

    [Fact]
    public void ReleaseHint_WhenDuplicate_ThrowsSessionDomainException()
    {
        var session = BuildActiveSession();
        var hintId = Guid.NewGuid();
        session.ReleaseHint(TeamId, hintId, NodeId, 5, SingleNodeRules());

        var act = () => session.ReleaseHint(TeamId, hintId, NodeId, 5, SingleNodeRules());

        act.Should().Throw<SessionDomainException>().WithMessage("*RB-04*");
    }

    [Fact]
    public void ReleaseHint_WhenNodeAlreadyPassed_ThrowsSessionDomainException()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 100), new AllowedNode(NodeB, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        var rules = new[]
        {
            new NodeValidationRule(NodeId, 1, NodeValidationType.Trivia, ["Answer"]),
            new NodeValidationRule(NodeB, 2, NodeValidationType.TreasureHunt, ["CODE"]),
        };
        session.SubmitTriviaAnswer(TeamId, NodeId, "Answer", 0, rules);

        var act = () => session.ReleaseHint(TeamId, Guid.NewGuid(), NodeId, 5, rules);

        act.Should().Throw<SessionDomainException>().WithMessage("*RN-04*");
    }

    [Fact]
    public void ReleaseHint_WhenNodeIsFuture_ThrowsSessionDomainException()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 100), new AllowedNode(NodeB, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        var rules = new[]
        {
            new NodeValidationRule(NodeId, 1, NodeValidationType.Trivia, ["Answer"]),
            new NodeValidationRule(NodeB, 2, NodeValidationType.TreasureHunt, ["CODE"]),
        };

        var act = () => session.ReleaseHint(TeamId, Guid.NewGuid(), NodeB, 5, rules);

        act.Should().Throw<SessionDomainException>().WithMessage("*RN-04*");
    }

    [Fact]
    public void ApplyManualPenalty_WhenActive_RaisesEvent()
    {
        var session = BuildActiveSession();

        session.ApplyManualPenalty(TeamId, session.OperatorRef, 15, "Comportamiento");

        session.DomainEvents.Should().Contain(e => e.GetType().Name == "ManualPenaltyAppliedEvent");
    }

    [Fact]
    public void PauseAndResume_WhenActive_TogglesStatus()
    {
        var session = BuildActiveSession();
        session.Pause("break");
        session.Status.Should().Be(LiveSessionStatus.Paused);
        session.Resume();
        session.Status.Should().Be(LiveSessionStatus.Active);
    }

    [Fact]
    public void Cancel_WhenActive_SetsCancelled()
    {
        var session = BuildActiveSession();
        session.Cancel();
        session.Status.Should().Be(LiveSessionStatus.Cancelled);
    }

    [Fact]
    public void SubmitTreasureHuntCode_WhenCorrect_ReturnsSuccess()
    {
        var treasureId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(treasureId, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var rules = new[] { new NodeValidationRule(treasureId, 1, NodeValidationType.TreasureHunt, ["CODE-123"]) };

        var result = session.SubmitTreasureHuntCode(TeamId, treasureId, "CODE-123", rules);

        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void ReleaseRemainingHintsAutomatically_WhenTreasureCompleted_ReleasesPendingWithoutPenalty()
    {
        var treasureId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(treasureId, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var rules = new[] { new NodeValidationRule(treasureId, 1, NodeValidationType.TreasureHunt, ["CODE-123"]) };
        session.SubmitTreasureHuntCode(TeamId, treasureId, "CODE-123", rules);

        var hintA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var hintB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        session.ReleaseRemainingHintsAutomatically(
            TeamId,
            treasureId,
            [(hintA, 1), (hintB, 2)]);

        session.ReleasedHints.Should().HaveCount(2);
        session.ReleasedHints.Should().OnlyContain(r =>
            !r.WasManualRelease && r.PenaltyPoints == 0);
        session.DomainEvents.Count(e => e.GetType().Name == "HintReleasedEvent").Should().Be(2);
    }

    [Fact]
    public void ReleaseRemainingHintsAutomatically_SkipsAlreadyReleasedHints()
    {
        var treasureId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(treasureId, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var rules = new[] { new NodeValidationRule(treasureId, 1, NodeValidationType.TreasureHunt, ["CODE-123"]) };

        var hintA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var hintB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        session.ReleaseHint(TeamId, hintA, treasureId, 10, rules, wasManualRelease: true, hintOrder: 1);
        session.SubmitTreasureHuntCode(TeamId, treasureId, "CODE-123", rules);

        session.ReleaseRemainingHintsAutomatically(
            TeamId,
            treasureId,
            [(hintA, 1), (hintB, 2)]);

        session.ReleasedHints.Should().HaveCount(2);
        session.ReleasedHints.Single(r => r.HintId == hintA).WasManualRelease.Should().BeTrue();
        session.ReleasedHints.Single(r => r.HintId == hintA).PenaltyPoints.Should().Be(10);
        session.ReleasedHints.Single(r => r.HintId == hintB).WasManualRelease.Should().BeFalse();
        session.ReleasedHints.Single(r => r.HintId == hintB).PenaltyPoints.Should().Be(0);
    }

    [Fact]
    public void ReleaseRemainingHintsAutomatically_WhenTreasureNotCompleted_Throws()
    {
        var treasureId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(treasureId, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        var act = () => session.ReleaseRemainingHintsAutomatically(
            TeamId,
            treasureId,
            [(Guid.NewGuid(), 1)]);

        act.Should().Throw<SessionDomainException>().WithMessage("*aún no completó*");
    }
}
