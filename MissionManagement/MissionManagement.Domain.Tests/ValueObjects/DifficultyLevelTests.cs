using FluentAssertions;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.ValueObjects;

public sealed class DifficultyLevelTests
{
    [Theory]
    [InlineData("Easy")]
    [InlineData("Medium")]
    [InlineData("Hard")]
    public void FromName_WhenKnown_ReturnsInstance(string name)
    {
        var level = DifficultyLevel.FromName(name);

        level.Name.Should().Be(name);
        level.ScoreMultiplier.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FromName_WhenUnknown_ThrowsArgumentOutOfRangeException()
    {
        var act = () => DifficultyLevel.FromName("Extreme");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToString_IncludesNameAndMultiplier()
    {
        DifficultyLevel.Medium.ToString().Should().Contain("Medium");
    }
}
