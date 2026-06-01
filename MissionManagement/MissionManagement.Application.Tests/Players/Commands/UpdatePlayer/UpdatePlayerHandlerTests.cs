using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Players.Commands.UpdatePlayer;
using Moq;

namespace MissionManagement.Application.Tests.Players.Commands.UpdatePlayer;

public sealed class UpdatePlayerHandlerTests
{
    private readonly Mock<IPlayerIdentityService> _playerIdentityServiceMock = new();

    [Fact]
    public async Task Handle_InvokesIdentityServiceWithExpectedPayload()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var command = new UpdatePlayerCommand(
            PlayerId: playerId,
            FirstName: "Luz",
            LastName: "Mena",
            Email: "luz@umbral.com");

        var handler = new UpdatePlayerHandler(_playerIdentityServiceMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _playerIdentityServiceMock.Verify(s => s.UpdatePlayerAsync(
            playerId,
            command.FirstName,
            command.LastName,
            command.Email,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
