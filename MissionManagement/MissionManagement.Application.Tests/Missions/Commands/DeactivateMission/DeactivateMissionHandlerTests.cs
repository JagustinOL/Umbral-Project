using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Commands.DeactivateMission;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;
using System.Net.Http;

namespace MissionManagement.Application.Tests.Missions.Commands.DeactivateMission;

public sealed class DeactivateMissionHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();
    private readonly Mock<ISessionValidationService> _sessionValidationMock = new();

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new DeactivateMissionHandler(_repositoryMock.Object, _sessionValidationMock.Object);
        var act = () => handler.Handle(new DeactivateMissionCommand(missionId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOpenSessions_ThrowsConflictException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out _);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _sessionValidationMock
            .Setup(s => s.HasOpenSessionsForMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeactivateMissionHandler(_repositoryMock.Object, _sessionValidationMock.Object);
        var act = () => handler.Handle(new DeactivateMissionCommand(mission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*sesiones abiertas*");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsConflictException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out _);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _sessionValidationMock
            .Setup(s => s.HasOpenSessionsForMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var handler = new DeactivateMissionHandler(_repositoryMock.Object, _sessionValidationMock.Object);
        var act = () => handler.Handle(new DeactivateMissionCommand(mission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*No fue posible validar sesiones*");
    }

    [Fact]
    public async Task Handle_WhenValid_DeactivatesAndSaves()
    {
        var mission = MissionTestData.CreateMissionWithStage(out _);
        mission.Activate();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);
        _sessionValidationMock
            .Setup(s => s.HasOpenSessionsForMissionAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeactivateMissionHandler(_repositoryMock.Object, _sessionValidationMock.Object);
        await handler.Handle(new DeactivateMissionCommand(mission.Id), CancellationToken.None);

        mission.Status.Should().Be(MissionManagement.Domain.Aggregates.MissionStatus.Inactive);
        _repositoryMock.Verify(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
    }
}
