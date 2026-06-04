using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Hints.Queries.GetHintsByNode;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Hints.Queries.GetHintsByNode;

public sealed class GetHintsByNodeHandlerTests
{
    [Fact]
    public async Task Handle_WhenNodeIsStage_ThrowsInvalidOperationException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetHintsByNodeHandler(repository.Object);
        var act = () => handler.Handle(new GetHintsByNodeQuery(mission.Id, stage.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTriviaNode_ReturnsHints()
    {
        var mission = MissionTestData.CreateMissionWithHint(out var node, out _);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetHintsByNodeHandler(repository.Object);
        var result = await handler.Handle(new GetHintsByNodeQuery(mission.Id, node.Id), CancellationToken.None);

        result.Should().ContainSingle(h => h.Content == "Pista inicial");
    }
}
