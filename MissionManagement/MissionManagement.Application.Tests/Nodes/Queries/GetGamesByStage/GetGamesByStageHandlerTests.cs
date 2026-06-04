using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Queries.GetGamesByStage;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Queries.GetGamesByStage;

public sealed class GetGamesByStageHandlerTests
{
    [Fact]
    public async Task Handle_WhenStageNotFound_ThrowsNotFoundException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out _);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetGamesByStageHandler(repository.Object);
        var act = () => handler.Handle(new GetGamesByStageQuery(mission.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReturnsChildGames()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        mission.AddTriviaNode(stage.Id, [new TriviaQuestion("Q", ["A", "B"], 0)], 1);

        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetGamesByStageHandler(repository.Object);
        var result = await handler.Handle(new GetGamesByStageQuery(mission.Id, stage.Id), CancellationToken.None);

        result.Should().ContainSingle(g => g.NodeType == "Trivia");
    }
}
