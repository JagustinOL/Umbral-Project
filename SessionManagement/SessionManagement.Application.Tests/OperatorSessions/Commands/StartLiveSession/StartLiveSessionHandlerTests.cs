using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.StartLiveSession;

public sealed class StartLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath_DelegatesToFacade()
    {
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.StartSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new StartLiveSessionHandler(facade.Object);
        await handler.Handle(new StartLiveSessionCommand(operatorId, sessionId), CancellationToken.None);

        facade.Verify(x => x.StartSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutTeams_ThrowsConflictException()
    {
        var operatorId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.StartSessionAsync(operatorId, sessionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("conflict"));

        var handler = new StartLiveSessionHandler(facade.Object);
        var act = () => handler.Handle(new StartLiveSessionCommand(operatorId, sessionId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
