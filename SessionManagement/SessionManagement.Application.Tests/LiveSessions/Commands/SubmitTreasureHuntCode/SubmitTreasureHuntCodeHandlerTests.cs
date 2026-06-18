using FluentAssertions;
using Moq;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Facades;
using SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Commands.SubmitTreasureHuntCode;

public sealed class SubmitTreasureHuntCodeHandlerTests
{
    [Fact]
    public async Task Handle_ShouldDelegateToFacade()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var facade = new Mock<ISessionOperationFacade>();
        facade.Setup(x => x.SubmitTreasureHuntAsync(sessionId, teamId, nodeId, "CODE-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubmissionResultDto(true, nodeId, null, 150));

        var handler = new SubmitTreasureHuntCodeHandler(facade.Object);
        var result = await handler.Handle(
            new SubmitTreasureHuntCodeCommand(sessionId, teamId, nodeId, "CODE-123"),
            CancellationToken.None);

        result.IsCorrect.Should().BeTrue();
    }
}
