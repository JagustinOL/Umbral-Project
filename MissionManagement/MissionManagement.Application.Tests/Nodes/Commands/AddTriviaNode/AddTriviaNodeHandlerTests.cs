using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Commands.AddTriviaNode;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Commands.AddTriviaNode;

public sealed class AddTriviaNodeHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var missionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var handler = new AddTriviaNodeHandler(_repositoryMock.Object);
        var command = new AddTriviaNodeCommand(
            MissionId: missionId,
            ParentNodeId: Guid.NewGuid(),
            Questions:
            [
                new TriviaQuestion("Pregunta", ["A", "B"], correctOptionIndex: 0)
            ],
            ExecutionOrder: 1);

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Id={missionId}*");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_InvokesDomainAndSaves()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new AddTriviaNodeHandler(_repositoryMock.Object);
        var command = new AddTriviaNodeCommand(
            MissionId: mission.Id,
            ParentNodeId: stage.Id,
            Questions:
            [
                new TriviaQuestion("Pregunta", ["A", "B"], correctOptionIndex: 0)
            ],
            ExecutionOrder: 1);

        // Act
        var nodeId = await handler.Handle(command, CancellationToken.None);

        // Assert
        nodeId.Should().NotBe(Guid.Empty);
        mission.FindNodeById(nodeId).Should().NotBeNull();
        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

