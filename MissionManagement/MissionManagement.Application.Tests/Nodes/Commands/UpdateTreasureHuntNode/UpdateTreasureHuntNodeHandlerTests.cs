using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Commands.UpdateTreasureHuntNode;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Commands.UpdateTreasureHuntNode;

public sealed class UpdateTreasureHuntNodeHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new UpdateTreasureHuntNodeHandler(_repositoryMock.Object);
        var act = () => handler.Handle(
            new UpdateTreasureHuntNodeCommand(missionId, Guid.NewGuid(), "Inst", "CODE", new GpsCoordinate(1, 2), 100),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesNodeBaseScoreAndSaves()
    {
        var mission = MissionTestData.CreateMissionWithTreasureHuntGame(out var node);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new UpdateTreasureHuntNodeHandler(_repositoryMock.Object);
        await handler.Handle(
            new UpdateTreasureHuntNodeCommand(
                mission.Id,
                node.Id,
                "Nuevas instrucciones",
                "XYZ",
                new GpsCoordinate(10, 20),
                BaseScore: 200),
            CancellationToken.None);

        node.Instructions.Should().Be("Nuevas instrucciones");
        node.SecretCode.Should().Be("XYZ");
        node.BaseScore.Should().Be(200);
        _repositoryMock.Verify(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
    }
}
