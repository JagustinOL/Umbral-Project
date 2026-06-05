using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionFullCoverageTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NodeB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Create_WhenInvalidArgs_Throws()
    {
        var nodes = new[] { new AllowedNode(NodeA, "Trivia", 10) };
        var act1 = () => LiveSession.Create(Guid.Empty, Guid.NewGuid(), nodes, 1m);
        act1.Should().Throw<ArgumentException>();
        var act2 = () => LiveSession.Create(Guid.NewGuid(), Guid.Empty, nodes, 1m);
        act2.Should().Throw<ArgumentException>();
        var act3 = () => LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), nodes, 0m);
        act3.Should().Throw<ArgumentOutOfRangeException>();
        var act4 = () => LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(), [], 1m);
        act4.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void RegisterTeam_WhenDuplicateOrWrongStatus_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);
        var dup = () => session.RegisterTeam(TeamId);
        dup.Should().Throw<SessionDomainException>();
        session.BeginPreparation();
        session.Start();
        var afterStart = () => session.RegisterTeam(Guid.NewGuid());
        afterStart.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void JoinTeam_WhenEmptyCode_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        var act = () => session.JoinTeam(Guid.NewGuid(), "  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AcceptEvidence_WhenNotActiveOrInvalidNode_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);
        var notActive = () => session.AcceptEvidence(TeamId, NodeA, "x");
        notActive.Should().Throw<SessionDomainException>().WithMessage("*RB-03*");

        session.BeginPreparation();
        session.Start();
        var badNode = () => session.AcceptEvidence(TeamId, Guid.NewGuid(), "x");
        badNode.Should().Throw<SessionDomainException>().WithMessage("*RB-05*");

        var unregistered = () => session.AcceptEvidence(Guid.NewGuid(), NodeA, "x");
        unregistered.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void MarkEvidence_WhenMissing_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var act = () => session.MarkEvidenceAsValid(Guid.NewGuid());
        act.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void GetCurrentNodeForTeam_WhenInvalidInput_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var rules = new[] { new NodeValidationRule(NodeA, 1, NodeValidationType.Trivia, "ok") };

        var unregistered = () => session.GetCurrentNodeForTeam(Guid.NewGuid(), rules);
        unregistered.Should().Throw<SessionDomainException>();

        var emptyRules = () => session.GetCurrentNodeForTeam(TeamId, []);
        emptyRules.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SubmitTrivia_WhenWrongOrderOrWrongAnswer_HandlesResult()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 50), new AllowedNode(NodeB, "TreasureHunt", 80)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var rules = new[]
        {
            new NodeValidationRule(NodeA, 1, NodeValidationType.Trivia, "Answer"),
            new NodeValidationRule(NodeB, 2, NodeValidationType.TreasureHunt, "CODE")
        };

        var wrongOrder = () => session.SubmitTreasureHuntCode(TeamId, NodeB, "CODE", rules);
        wrongOrder.Should().Throw<SessionDomainException>().WithMessage("*RN-11*");

        var wrongAnswer = session.SubmitTriviaAnswer(TeamId, NodeA, "bad", rules);
        wrongAnswer.IsCorrect.Should().BeFalse();
        wrongAnswer.AwardedPoints.Should().Be(0);

        var correct = session.SubmitTriviaAnswer(TeamId, NodeA, "Answer", rules);
        correct.IsCorrect.Should().BeTrue();
        correct.NextNodeId.Should().Be(NodeB);
    }

    [Fact]
    public void SubmitTrivia_WhenStageAlreadyClosed_ThrowsRn04()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10), new AllowedNode(NodeB, "Trivia", 20)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        var rules = new[]
        {
            new NodeValidationRule(NodeA, 1, NodeValidationType.Trivia, "ok"),
            new NodeValidationRule(NodeB, 2, NodeValidationType.Trivia, "next")
        };
        session.SubmitTriviaAnswer(TeamId, NodeA, "ok", rules);
        var act = () => session.SubmitTriviaAnswer(TeamId, NodeA, "ok", rules);
        act.Should().Throw<SessionDomainException>().WithMessage("*RN-04*");
    }

    [Fact]
    public void ReleaseHint_WhenNotActive_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        var act = () => session.ReleaseHint(TeamId, Guid.NewGuid(), NodeA, 5);
        act.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void ApplyManualPenalty_WhenNotActiveOrZeroPoints_Throws()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);
        var notActive = () => session.ApplyManualPenalty(TeamId, session.OperatorRef, 10, "reason");
        notActive.Should().Throw<SessionDomainException>();

        session.BeginPreparation();
        session.Start();
        var zero = () => session.ApplyManualPenalty(TeamId, session.OperatorRef, 0, "reason");
        zero.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Finalize_FromPaused_Works()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeA, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        session.Pause();
        session.Finalize("done");
        session.Status.Should().Be(LiveSessionStatus.Finalized);
        session.DomainEvents.Should().Contain(e => e.GetType().Name == "SessionFinalizedEvent");
    }
}
