using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Commands.AddRootNode;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Commands.AddRootNode;

public sealed class AddRootNodeHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var missionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var handler = new AddRootNodeHandler(_repositoryMock.Object);
        var command = new AddRootNodeCommand(
            MissionId: missionId,
            Title: "Etapa 1",
            Description: "Descripción",
            ExecutionOrder: 1);

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
    public async Task Handle_WhenValidCommand_SavesAndReturnsId()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _repositoryMock
            .Setup(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new AddRootNodeHandler(_repositoryMock.Object);
        var command = new AddRootNodeCommand(
            MissionId: mission.Id,
            Title: "Etapa 1",
            Description: "Descripción etapa",
            ExecutionOrder: 1);

        // Act
        var nodeId = await handler.Handle(command, CancellationToken.None);

        // Assert
        nodeId.Should().NotBe(Guid.Empty);
        mission.Nodes.Should().HaveCount(1);
        mission.Nodes[0].Title.Should().Be("Etapa 1");

        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
