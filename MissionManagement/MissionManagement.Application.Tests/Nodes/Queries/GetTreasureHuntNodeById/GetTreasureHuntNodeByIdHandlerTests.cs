using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Queries.GetTreasureHuntNodeById;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Queries.GetTreasureHuntNodeById;

public sealed class GetTreasureHuntNodeByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenWrongNodeType_ThrowsConflictException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetTreasureHuntNodeByIdHandler(repository.Object);
        var act = () => handler.Handle(new GetTreasureHuntNodeByIdQuery(mission.Id, stage.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenTreasureHunt_ReturnsDto()
    {
        var mission = MissionTestData.CreateMissionWithTreasureHuntGame(out var node);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetTreasureHuntNodeByIdHandler(repository.Object);
        var result = await handler.Handle(new GetTreasureHuntNodeByIdQuery(mission.Id, node.Id), CancellationToken.None);

        result.SecretCode.Should().Be("ABC123");
        result.Destination.Latitude.Should().Be(4.711);
    }
}
