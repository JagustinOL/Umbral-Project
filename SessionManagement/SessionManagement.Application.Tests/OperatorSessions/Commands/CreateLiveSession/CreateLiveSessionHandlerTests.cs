using FluentAssertions;
using Moq;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Facades;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.CreateLiveSession;

public sealed class CreateLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenMissionNotAssigned_ThrowsNotFoundException()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.CreateSessionAsync(operatorId, missionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("not found"));

        var handler = new CreateLiveSessionHandler(facade.Object);
        var act = () => handler.Handle(new CreateLiveSessionCommand(operatorId, missionId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMissionNotActive_ThrowsConflictException()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.CreateSessionAsync(operatorId, missionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("conflict"));

        var handler = new CreateLiveSessionHandler(facade.Object);
        var act = () => handler.Handle(new CreateLiveSessionCommand(operatorId, missionId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenValid_DelegatesToFacade()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.CreateSessionAsync(operatorId, missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatedLiveSessionDto(sessionId, "ABC123"));

        var handler = new CreateLiveSessionHandler(facade.Object);
        var result = await handler.Handle(new CreateLiveSessionCommand(operatorId, missionId), CancellationToken.None);

        result.SessionId.Should().Be(sessionId);
        result.JoinCode.Should().Be("ABC123");
    }
}
