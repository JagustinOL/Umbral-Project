using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Commands.DeleteNode;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Commands.DeleteNode;

public sealed class DeleteNodeHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var handler = new DeleteNodeHandler(_repositoryMock.Object);
        var command = new DeleteNodeCommand(missionId, nodeId);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Id={missionId}*");

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_SavesMission()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 0);
        mission.AddRootNode(stage);
        var nodeId = stage.Id;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _repositoryMock
            .Setup(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteNodeHandler(_repositoryMock.Object);
        var command = new DeleteNodeCommand(mission.Id, nodeId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mission.Nodes.Should().BeEmpty();

        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
