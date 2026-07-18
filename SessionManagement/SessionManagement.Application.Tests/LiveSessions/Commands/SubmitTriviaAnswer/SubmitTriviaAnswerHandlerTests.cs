using FluentAssertions;
using Moq;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Facades;
using SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Commands.SubmitTriviaAnswer;

public sealed class SubmitTriviaAnswerHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToFacade()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.SubmitTriviaAsync(sessionId, teamId, nodeId, "Bogota", 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionResultDto(true, nodeId, Guid.NewGuid(), 100, 0, 1, true));

        var handler = new SubmitTriviaAnswerHandler(facade.Object);
        var result = await handler.Handle(
            new SubmitTriviaAnswerCommand(sessionId, teamId, nodeId, "Bogota", 0),
            CancellationToken.None);

        result.IsCorrect.Should().BeTrue();
        facade.Verify(
            x => x.SubmitTriviaAsync(sessionId, teamId, nodeId, "Bogota", 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
