using FluentAssertions;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Hints;
using MissionManagement.Application.Hints.Queries.GetHintsByNode;
using MissionManagement.Application.Tests.Support;
using Moq;

namespace MissionManagement.Application.Tests.Hints.Queries.GetHintsByNode;

public sealed class GetHintsByNodeHandlerTests
{
    [Fact]
    public async Task Handle_WhenNodeIsStage_ThrowsInvalidOperationException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var hintAccess = new Mock<IHintAccessService>();
        hintAccess
            .Setup(s => s.GetHintsForNodeAsync(mission.Id, stage.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Solo nodos Trivia o Treasure Hunt admiten pistas."));

        var handler = new GetHintsByNodeHandler(hintAccess.Object);
        var act = () => handler.Handle(new GetHintsByNodeQuery(mission.Id, stage.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_WhenTriviaNode_ReturnsHints()
    {
        var mission = MissionTestData.CreateMissionWithHint(out var node, out _);
        var hintAccess = new Mock<IHintAccessService>();
        hintAccess
            .Setup(s => s.GetHintsForNodeAsync(mission.Id, node.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HintDto>
            {
                new(Guid.NewGuid(), 1, "Pista inicial", 0)
            });

        var handler = new GetHintsByNodeHandler(hintAccess.Object);
        var result = await handler.Handle(new GetHintsByNodeQuery(mission.Id, node.Id), CancellationToken.None);

        result.Should().ContainSingle(h => h.Content == "Pista inicial");
    }
}
