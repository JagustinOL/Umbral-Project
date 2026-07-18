using FluentAssertions;
using Moq;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.FinalizeLiveSession;

public sealed class FinalizeLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenValid_DelegatesToFacade()
    {
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.FinalizeSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FinalizeLiveSessionHandler(facade.Object);
        await handler.Handle(new FinalizeLiveSessionCommand(operatorId, sessionId), CancellationToken.None);

        facade.Verify(x => x.FinalizeSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
