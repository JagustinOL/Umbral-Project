using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionMultiQuestionTriviaTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TriviaNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TreasureNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void SubmitTriviaAnswer_WithMultipleQuestions_ShouldAdvanceOnlyAfterLastQuestion()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            1.0m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(TriviaNodeId, 1, NodeValidationType.Trivia, ["Answer1", "Answer2"]),
            new NodeValidationRule(TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];

        var first = session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Answer1", 0, rules);
        first.IsCorrect.Should().BeTrue();
        first.NodeCompleted.Should().BeFalse();
        first.NextNodeId.Should().Be(TriviaNodeId);
        first.AwardedPoints.Should().Be(0);

        var second = session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Answer2", 1, rules);
        second.IsCorrect.Should().BeTrue();
        second.NodeCompleted.Should().BeTrue();
        second.NextNodeId.Should().Be(TreasureNodeId);
        second.AwardedPoints.Should().Be(100);
    }

    [Fact]
    public void SubmitTriviaAnswer_WhenSkippingQuestionIndex_ShouldThrow()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(TriviaNodeId, "Trivia", 100)],
            1.0m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(TriviaNodeId, 1, NodeValidationType.Trivia, ["Answer1", "Answer2"])
        ];

        var act = () => session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Answer2", 1, rules);
        act.Should().Throw<SessionDomainException>().WithMessage("*pregunta 0*");
    }

    [Fact]
    public void SubmitTriviaAnswer_WhenWrongOnFirstQuestion_ClosesNodeWithoutRetry()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            1.0m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(TriviaNodeId, 1, NodeValidationType.Trivia, ["Answer1", "Answer2"]),
            new NodeValidationRule(TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];

        var wrong = session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "bad", 0, rules);
        wrong.IsCorrect.Should().BeFalse();
        wrong.NodeCompleted.Should().BeTrue();
        wrong.AwardedPoints.Should().Be(0);
        wrong.NextNodeId.Should().Be(TreasureNodeId);

        var retry = () => session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Answer1", 0, rules);
        retry.Should().Throw<SessionDomainException>().WithMessage("*ya está cerrada*");
    }
}
