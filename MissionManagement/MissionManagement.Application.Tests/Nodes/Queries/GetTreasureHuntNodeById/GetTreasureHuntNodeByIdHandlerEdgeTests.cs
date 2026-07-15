using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Queries.GetTreasureHuntNodeById;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Queries.GetTreasureHuntNodeById;

public sealed class GetTreasureHuntNodeByIdHandlerEdgeTests
{
    [Fact]
    public async Task Handle_WhenMissingDestination_ThrowsConflict()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var nodeId = mission.AddTreasureHuntNode(stage.Id, "Go", "CODE", new GpsCoordinate(1, 2), 1, baseScore: 100);
        var node = mission.FindNodeById(nodeId)!;
        typeof(MissionManagement.Domain.Entities.MissionNode)
            .GetProperty(nameof(MissionManagement.Domain.Entities.MissionNode.Destination))!
            .SetValue(node, null);

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var handler = new GetTreasureHuntNodeByIdHandler(repo.Object);
        var act = () => handler.Handle(new GetTreasureHuntNodeByIdQuery(mission.Id, nodeId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*coordenadas*");
    }
}
