using FluentAssertions;
using MissionManagement.Application.Common;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Operators.Commands.CreateOperator;
using Moq;

namespace MissionManagement.Application.Tests.Operators.Commands.CreateOperator;

public sealed class CreateOperatorHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();

    [Fact]
    public async Task Handle_WhenValidCommand_InvokesCreateOperatorOnceAndReturnsResult()
    {
        var expected = new CreateOperatorResult(Guid.NewGuid(), "WXYZ-9876");
        var command = new CreateOperatorCommand(
            FirstName: "Ada",
            LastName: "Lovelace",
            Email: "ada@umbral.com");

        _identityServiceMock
            .Setup(s => s.CreateOperatorAsync(
                command.FirstName,
                command.LastName,
                command.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new CreateOperatorHandler(_identityServiceMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expected);
        _identityServiceMock.Verify(
            s => s.CreateOperatorAsync(
                command.FirstName,
                command.LastName,
                command.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
