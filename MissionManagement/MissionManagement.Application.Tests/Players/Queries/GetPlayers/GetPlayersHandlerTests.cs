using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Players.Queries.GetPlayers;
using Moq;

namespace MissionManagement.Application.Tests.Players.Queries.GetPlayers;

public sealed class GetPlayersHandlerTests
{
    private readonly Mock<IPlayerIdentityService> _playerIdentityServiceMock = new();

    [Fact]
    public async Task Handle_ReturnsPlayersFromIdentityService()
    {
        // Arrange
        IReadOnlyList<PlayerIdentityDto> players =
        [
            new(Guid.NewGuid(), "Ana", "Diaz", "ana@umbral.com", true)
        ];

        _playerIdentityServiceMock
            .Setup(s => s.GetPlayersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(players);

        var handler = new GetPlayersHandler(_playerIdentityServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetPlayersQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(players);
    }
}
