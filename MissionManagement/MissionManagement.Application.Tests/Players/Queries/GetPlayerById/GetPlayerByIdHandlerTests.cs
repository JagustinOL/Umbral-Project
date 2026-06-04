using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Players.Queries.GetPlayerById;
using Moq;

namespace MissionManagement.Application.Tests.Players.Queries.GetPlayerById;

public sealed class GetPlayerByIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPlayerFromIdentityService()
    {
        var playerId = Guid.NewGuid();
        var expected = new PlayerIdentityDto(playerId, "Ana", "García", "ana@umbral.com", true);
        var service = new Mock<IPlayerIdentityService>();
        service.Setup(s => s.GetPlayerByIdAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetPlayerByIdHandler(service.Object);
        var result = await handler.Handle(new GetPlayerByIdQuery(playerId), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }
}
