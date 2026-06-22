using FluentAssertions;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;

namespace UserService.Domain.Tests.Aggregates;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidData_ReturnsActiveUser()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var user = User.Create(id, "player@umbral.com", "Ana", "Lopez", UserRole.Player);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be("player@umbral.com");
        user.Role.Should().Be(UserRole.Player);
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenActive_SetsInactiveStatus()
    {
        // Arrange
        var user = User.Create(Guid.NewGuid(), "op@umbral.com", "Luis", "Perez", UserRole.Operator);

        // Act
        user.Deactivate();

        // Assert
        user.IsActive.Should().BeFalse();
    }
}
