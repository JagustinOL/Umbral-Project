using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Queries.GetNodesByMission;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Queries.GetNodesByMission;

public sealed class GetNodesByMissionHandlerTests
{
    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new GetNodesByMissionHandler(repository.Object);
        var act = () => handler.Handle(new GetNodesByMissionQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReturnsStageNodes()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetNodesByMissionHandler(repository.Object);
        var result = await handler.Handle(new GetNodesByMissionQuery(mission.Id), CancellationToken.None);

        result.Should().ContainSingle(n => n.Id == stage.Id && n.NodeType == "Stage");
    }
}
