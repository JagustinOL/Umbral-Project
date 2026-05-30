using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Operators.Commands.CreateOperator;
using Moq;

namespace MissionManagement.Application.Tests.Operators.Commands.CreateOperator;

public sealed class CreateOperatorHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();

    [Fact]
    public async Task Handle_WhenValidCommand_InvokesCreateOperatorOnceAndReturnsId()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
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
            .ReturnsAsync(operatorId);

        var handler = new CreateOperatorHandler(_identityServiceMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(operatorId);
        _identityServiceMock.Verify(
            s => s.CreateOperatorAsync(
                command.FirstName,
                command.LastName,
                command.Email,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

