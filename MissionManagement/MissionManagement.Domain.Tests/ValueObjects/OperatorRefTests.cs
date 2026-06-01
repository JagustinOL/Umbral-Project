using FluentAssertions;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.ValueObjects;

public sealed class OperatorRefTests
{
    [Fact]
    public void Constructor_WhenOperatorIdIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var operatorId = Guid.Empty;

        // Act
        var action = () => new OperatorRef(operatorId);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*operatorId no puede ser vacio*");
    }

    [Fact]
    public void Constructor_WhenOperatorIdIsValid_CreatesOperatorRef()
    {
        // Arrange
        var operatorId = Guid.NewGuid();

        // Act
        var result = new OperatorRef(operatorId);

        // Assert
        result.OperatorId.Should().Be(operatorId);
    }
}

