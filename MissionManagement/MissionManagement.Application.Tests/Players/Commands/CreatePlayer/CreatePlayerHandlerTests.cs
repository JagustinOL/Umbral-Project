using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Players.Commands.CreatePlayer;
using Moq;

namespace MissionManagement.Application.Tests.Players.Commands.CreatePlayer;

public sealed class CreatePlayerHandlerTests
{
    private readonly Mock<IPlayerIdentityService> _playerIdentityServiceMock = new();

    [Fact]
    public async Task Handle_WhenValidCommand_InvokesCreatePlayerAndReturnsId()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var command = new CreatePlayerCommand(
            FirstName: "Lina",
            LastName: "Ramos",
            Email: "lina@umbral.com",
            Password: "SecurePass1");

        _playerIdentityServiceMock
            .Setup(s => s.CreatePlayerAsync(
                command.FirstName,
                command.LastName,
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(playerId);

        var handler = new CreatePlayerHandler(_playerIdentityServiceMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(playerId);
        _playerIdentityServiceMock.Verify(
            s => s.CreatePlayerAsync(
                command.FirstName,
                command.LastName,
                command.Email,
                command.Password,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
