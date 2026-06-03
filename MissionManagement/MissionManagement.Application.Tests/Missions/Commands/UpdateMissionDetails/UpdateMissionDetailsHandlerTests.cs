using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Commands.UpdateMissionDetails;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Commands.UpdateMissionDetails;

public sealed class UpdateMissionDetailsHandlerTests
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

        var handler = new UpdateMissionDetailsHandler(_repositoryMock.Object);
        var command = new UpdateMissionDetailsCommand(
            Id: missionId,
            Title: "Título",
            Description: "Descripción",
            MaxDurationMinutes: null);

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
        var mission = Mission.Create("Original", "Descripción", DifficultyLevel.Easy);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        _repositoryMock
            .Setup(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateMissionDetailsHandler(_repositoryMock.Object);
        var command = new UpdateMissionDetailsCommand(
            Id: mission.Id,
            Title: "Actualizado",
            Description: "Nueva descripción",
            MaxDurationMinutes: 45);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        mission.Title.Should().Be("Actualizado");
        mission.Description.Should().Be("Nueva descripción");
        mission.MaxDurationMinutes.Should().Be(45);

        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
