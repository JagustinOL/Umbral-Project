using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Commands.AssignOperatorToMission;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Commands.AssignOperatorToMission;

public sealed class AssignOperatorToMissionHandlerTests
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

        var handler = new AssignOperatorToMissionHandler(_repositoryMock.Object);
        var command = new AssignOperatorToMissionCommand(
            MissionId: missionId,
            OperatorId: Guid.NewGuid());

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
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var operatorId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new AssignOperatorToMissionHandler(_repositoryMock.Object);
        var command = new AssignOperatorToMissionCommand(
            MissionId: mission.Id,
            OperatorId: operatorId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mission.Operators.Should().ContainSingle(x => x.OperatorId == operatorId);
        mission.Status.Should().Be(MissionStatus.Active);
        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

