using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Queries.GetNodePlayerContent;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Queries.GetNodePlayerContent;

public sealed class GetNodePlayerContentHandlerTests
{
    [Fact]
    public async Task Handle_WhenTrivia_ReturnsQuestionsWithoutCorrectIndex()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        mission.AddTriviaNode(
            stage.Id,
            [
                new MissionManagement.Domain.ValueObjects.TriviaQuestion("Q1", ["A", "B"], 0),
                new MissionManagement.Domain.ValueObjects.TriviaQuestion("Q2", ["C", "D"], 1),
            ],
            1,
            baseScore: 100);

        var triviaNode = mission.Nodes.SelectMany(n => n.Children).First(n => n.NodeType.ToString() == "Trivia");
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetNodePlayerContentHandler(repository.Object);
        var result = await handler.Handle(
            new GetNodePlayerContentQuery(mission.Id, triviaNode.Id),
            CancellationToken.None);

        result.Questions.Should().HaveCount(2);
        result.Questions![0].Prompt.Should().Be("Q1");
        result.Questions[0].Options.Should().Equal("A", "B");
        result.Instructions.Should().BeNull();
        result.Destination.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTreasureHunt_ReturnsInstructionsWithoutSecretCode()
    {
        var mission = MissionTestData.CreateMissionWithTreasureHuntGame(out var node);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetNodePlayerContentHandler(repository.Object);
        var result = await handler.Handle(
            new GetNodePlayerContentQuery(mission.Id, node.Id),
            CancellationToken.None);

        result.Instructions.Should().NotBeNullOrWhiteSpace();
        result.Questions.Should().BeNull();
        result.Destination.Should().NotBeNull();
        result.Destination!.Latitude.Should().Be(4.711);
        result.Destination.Longitude.Should().Be(-74.0721);
    }

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new GetNodePlayerContentHandler(repository.Object);
        var act = () => handler.Handle(
            new GetNodePlayerContentQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
