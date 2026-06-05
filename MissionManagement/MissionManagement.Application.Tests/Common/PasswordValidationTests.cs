using FluentAssertions;
using MissionManagement.Application.Common;

namespace MissionManagement.Application.Tests.Common;

public sealed class PasswordValidationTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureValid_WhenTooShort_ThrowsArgumentException(string password)
    {
        var act = () => PasswordValidation.EnsureValid(password);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*al menos 8*");
    }

    [Fact]
    public void EnsureValid_WhenValid_DoesNotThrow()
    {
        var act = () => PasswordValidation.EnsureValid("ValidPass1");

        act.Should().NotThrow();
    }
}
