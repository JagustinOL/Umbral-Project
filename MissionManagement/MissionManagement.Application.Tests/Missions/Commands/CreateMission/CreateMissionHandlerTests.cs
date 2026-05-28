using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Commands.CreateMission;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Commands.CreateMission;

public sealed class CreateMissionHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenTitleAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TitleExistsAsync("Misión duplicada", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateMissionHandler(_repositoryMock.Object);
        var command = new CreateMissionCommand(
            Title: "Misión duplicada",
            Description: "Descripción",
            Difficulty: 2,
            MaxDurationMinutes: null);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Ya existe una misión*");

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_SavesAndReturnsId()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.TitleExistsAsync("Nueva misión", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mission? savedMission = null;
        _repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()))
            .Callback<Mission, CancellationToken>((m, _) => savedMission = m)
            .Returns(Task.CompletedTask);

        var handler = new CreateMissionHandler(_repositoryMock.Object);
        var command = new CreateMissionCommand(
            Title: "Nueva misión",
            Description: "Descripción válida",
            Difficulty: 2,
            MaxDurationMinutes: 120);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        savedMission.Should().NotBeNull();
        savedMission!.Title.Should().Be("Nueva misión");
        savedMission.Status.Should().Be(MissionStatus.Draft);

        _repositoryMock.Verify(
            r => r.SaveAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
