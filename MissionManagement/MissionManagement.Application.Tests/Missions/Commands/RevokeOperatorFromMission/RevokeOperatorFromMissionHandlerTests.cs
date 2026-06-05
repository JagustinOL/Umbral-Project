using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Commands.RevokeOperatorFromMission;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Commands.RevokeOperatorFromMission;

public sealed class RevokeOperatorFromMissionHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();
    private readonly Mock<ISessionValidationService> _sessionValidationServiceMock = new();

    [Fact]
    public async Task Handle_WhenOperatorIsSupervisingMission_ThrowsConflictException()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();

        _sessionValidationServiceMock
            .Setup(s => s.IsSupervisingMissionAsync(operatorId, missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RevokeOperatorFromMissionHandler(
            _repositoryMock.Object,
            _sessionValidationServiceMock.Object);
        var command = new RevokeOperatorFromMissionCommand(
            MissionId: missionId,
            OperatorId: operatorId);

        // Act
        var action = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("*supervisa una sesión activa*");

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_RevokesOperatorAndSaves()
    {
        // Arrange
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var operatorId = Guid.NewGuid();
        mission.AssignOperator(operatorId);

        _sessionValidationServiceMock
            .Setup(s => s.IsSupervisingMissionAsync(operatorId, mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new RevokeOperatorFromMissionHandler(
            _repositoryMock.Object,
            _sessionValidationServiceMock.Object);
        var command = new RevokeOperatorFromMissionCommand(
            MissionId: mission.Id,
            OperatorId: operatorId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mission.Operators.Should().NotContain(x => x.OperatorId == operatorId);
        mission.Status.Should().Be(MissionStatus.Draft);
        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

