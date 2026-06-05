using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Operators.Commands.DeactivateOperator;
using Moq;
using System.Net.Http;

namespace MissionManagement.Application.Tests.Operators.Commands.DeactivateOperator;

public sealed class DeactivateOperatorHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<ISessionValidationService> _sessionValidationServiceMock = new();

    [Fact]
    public async Task Handle_WhenOperatorHasActiveSessions_ThrowsConflictException()
    {
        // Arrange
        var operatorId = Guid.NewGuid();

        _sessionValidationServiceMock
            .Setup(s => s.HasActiveSessionsAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeactivateOperatorHandler(
            _identityServiceMock.Object,
            _sessionValidationServiceMock.Object);
        var command = new DeactivateOperatorCommand(operatorId);

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("*sesiones activas*");

        _identityServiceMock.Verify(
            s => s.DeactivateOperatorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOperatorHasNoActiveSessions_InvokesDeactivateOperatorOnce()
    {
        // Arrange
        var operatorId = Guid.NewGuid();

        _sessionValidationServiceMock
            .Setup(s => s.HasActiveSessionsAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeactivateOperatorHandler(
            _identityServiceMock.Object,
            _sessionValidationServiceMock.Object);
        var command = new DeactivateOperatorCommand(operatorId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(
            s => s.DeactivateOperatorAsync(operatorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSessionValidationFails_ThrowsConflictException()
    {
        var operatorId = Guid.NewGuid();
        _sessionValidationServiceMock
            .Setup(s => s.HasActiveSessionsAsync(operatorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("down"));

        var handler = new DeactivateOperatorHandler(
            _identityServiceMock.Object,
            _sessionValidationServiceMock.Object);
        var act = () => handler.Handle(new DeactivateOperatorCommand(operatorId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*No fue posible validar sesiones*");
    }
}

