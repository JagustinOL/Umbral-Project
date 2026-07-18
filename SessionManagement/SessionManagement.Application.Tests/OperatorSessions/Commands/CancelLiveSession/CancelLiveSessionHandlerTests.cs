using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.CancelLiveSession;

public sealed class CancelLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionNotFound_ThrowsNotFoundException()
    {
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.CancelSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("not found"));

        var handler = new CancelLiveSessionHandler(facade.Object);
        var act = () => handler.Handle(new CancelLiveSessionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_DelegatesToFacade()
    {
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.CancelSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CancelLiveSessionHandler(facade.Object);
        await handler.Handle(new CancelLiveSessionCommand(operatorId, sessionId), CancellationToken.None);

        facade.Verify(x => x.CancelSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
