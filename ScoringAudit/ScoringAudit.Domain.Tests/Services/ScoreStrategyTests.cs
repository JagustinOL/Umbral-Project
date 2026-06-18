using ScoringAudit.Domain.Services;
using Xunit;

namespace ScoringAudit.Domain.Tests.Services;

public sealed class TriviaScoreStrategyTests
{
    [Fact]
    public void Calculate_AppliesSpeedBonus_ForFastAnswers()
    {
        var strategy = new TriviaScoreStrategy();
        var score = strategy.Calculate(100, 1.5m, 20);
        Assert.Equal(180, score);
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

        Assert.Equal(60, origin.ComputedScore);
    }
}
