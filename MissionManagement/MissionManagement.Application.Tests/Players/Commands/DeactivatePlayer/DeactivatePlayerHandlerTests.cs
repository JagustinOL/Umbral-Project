using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Players.Commands.DeactivatePlayer;
using Moq;

namespace MissionManagement.Application.Tests.Players.Commands.DeactivatePlayer;

public sealed class DeactivatePlayerHandlerTests
{
    [Fact]
    public async Task Handle_InvokesDeactivatePlayerOnce()
    {
        var playerId = Guid.NewGuid();
        var serviceMock = new Mock<IPlayerIdentityService>();
        var handler = new DeactivatePlayerHandler(serviceMock.Object);

        await handler.Handle(new DeactivatePlayerCommand(playerId), CancellationToken.None);

        serviceMock.Verify(s => s.DeactivatePlayerAsync(playerId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
