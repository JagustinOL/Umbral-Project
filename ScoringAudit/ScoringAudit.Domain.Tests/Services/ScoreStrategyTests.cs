using ScoringAudit.Domain.Services;
using Xunit;

namespace ScoringAudit.Domain.Tests.Services;

public sealed class TriviaScoreStrategyTests
{
    [Fact]
    public void Calculate_UsesBaseTimesDifficulty_IgnoringElapsedTime()
    {
        var strategy = new TriviaScoreStrategy();
        // Antes: 100 × 1.5 × 1.2 (speed) = 180 — incorrecto frente a RN-09.
        var score = strategy.Calculate(100, 1.5m, 20);
        Assert.Equal(150, score);
    }

    [Fact]
    public void Calculate_WithBaseScoreFifteen_ReturnsFifteenOnEasyDifficulty()
    {
        var strategy = new TriviaScoreStrategy();
        Assert.Equal(15, strategy.Calculate(15, 1.0m, 5));
        Assert.Equal(15, strategy.Calculate(15, 1.0m, 500));
    }

    [Fact]
    public void TreasureHuntStrategy_DoesNotApplySpeedBonus()
    {
        var strategy = new TreasureHuntScoreStrategy();
        var score = strategy.Calculate(100, 2.0m, 500);
        Assert.Equal(200, score);
    }

    [Fact]
    public void ScoreCalculatorService_SelectsStrategyByNodeType()
    {
        var calculator = new ScoreCalculatorService([
            new TriviaScoreStrategy(),
            new TreasureHuntScoreStrategy()
        ]);

        var origin = calculator.Calculate(
            Guid.NewGuid(),
            "Trivia",
            50,
            1.0m,
            25);

        Assert.Equal(50, origin.ComputedScore);
    }
}
