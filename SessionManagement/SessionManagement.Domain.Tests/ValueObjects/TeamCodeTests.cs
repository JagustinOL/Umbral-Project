using FluentAssertions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.ValueObjects;

public sealed class TeamCodeTests
{
    [Fact]
    public void From_WhenValid_ReturnsUppercaseCode()
    {
        var code = TeamCode.From("ab12cd");
        code.Value.Should().Be("AB12CD");
        code.ToString().Should().Be("AB12CD");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SHORT")]
    [InlineData("TOOLONG1")]
    [InlineData("abc-12")]
    public void From_WhenInvalid_Throws(string value)
    {
        var act = () => TeamCode.From(value);
        act.Should().Throw<ArgumentException>();
    }
}
