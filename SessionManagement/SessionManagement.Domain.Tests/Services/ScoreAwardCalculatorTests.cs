using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Services;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Services;

public sealed class ScoreAwardCalculatorTests
{
    [Theory]
    [InlineData(100, 1.0, 100)]
    [InlineData(100, 1.5, 150)]
    [InlineData(100, 2.0, 200)]
    [InlineData(0, 2.0, 0)]
    [InlineData(-5, 2.0, 0)]
    [InlineData(1, 1.5, 2)]
    [InlineData(2, 1.25, 3)]
    public void Compute_MatchesScoringAuditFormula(int baseScore, double multiplier, int expected)
    {
        ScoreAwardCalculator.Compute(baseScore, (decimal)multiplier).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Compute_WhenMultiplierNotPositive_Throws(double multiplier)
    {
        var act = () => ScoreAwardCalculator.Compute(100, (decimal)multiplier);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public sealed class LiveSessionAwardedPointsConsistencyTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TriviaNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TreasureNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void SubmitTreasureHunt_WithHardMultiplier_ReturnsAwardedPointsTimesTwo()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(TreasureNodeId, "TreasureHunt", 100),
                new AllowedNode(TriviaNodeId, "Trivia", 100)
            ],
            2.0m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(TreasureNodeId, 1, NodeValidationType.TreasureHunt, ["CODE-123"]),
            new NodeValidationRule(TriviaNodeId, 2, NodeValidationType.Trivia, ["Bogota"])
        ];

        var result = session.SubmitTreasureHuntCode(TeamId, TreasureNodeId, "CODE-123", rules);
        result.IsCorrect.Should().BeTrue();
        result.NodeCompleted.Should().BeTrue();
        result.AwardedPoints.Should().Be(200);
    }

    [Fact]
    public void SubmitTrivia_WithHardMultiplier_ReturnsAwardedPointsTimesTwoOnNodeComplete()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            2.0m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"]),
            new NodeValidationRule(TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];

        var result = session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Bogota", 0, rules);
        result.IsCorrect.Should().BeTrue();
        result.NodeCompleted.Should().BeTrue();
        result.AwardedPoints.Should().Be(200);
    }

    [Fact]
    public void SubmitTrivia_WhenWrong_StillAwardsZeroRegardlessOfMultiplier()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            2.0m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        IReadOnlyList<NodeValidationRule> rules =
        [
            new NodeValidationRule(TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"]),
            new NodeValidationRule(TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];

        var result = session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Wrong", 0, rules);
        result.IsCorrect.Should().BeFalse();
        result.NodeCompleted.Should().BeTrue();
        result.AwardedPoints.Should().Be(0);
    }
}
