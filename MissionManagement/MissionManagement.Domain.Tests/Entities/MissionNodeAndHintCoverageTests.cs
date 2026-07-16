using FluentAssertions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Entities;

public sealed class MissionNodeAndHintCoverageTests
{
    [Fact]
    public void CreateTreasureHunt_WhenValid_TrimsAndSetsFields()
    {
        var parent = Guid.NewGuid();
        var node = MissionNode.CreateTreasureHunt(
            1,
            "  Ve al patio  ",
            "  CODE  ",
            new GpsCoordinate(10, -70),
            parent,
            50);

        node.Instructions.Should().Be("Ve al patio");
        node.SecretCode.Should().Be("CODE");
        node.ParentNodeId.Should().Be(parent);
        node.BaseScore.Should().Be(50);
        node.NodeType.Should().Be(MissionNodeType.TreasureHunt);
    }

    [Fact]
    public void CreateTreasureHunt_WhenInvalid_Throws()
    {
        var gps = new GpsCoordinate(1, 2);
        var actParent = () => MissionNode.CreateTreasureHunt(1, "i", "c", gps, Guid.Empty, 10);
        var actInst = () => MissionNode.CreateTreasureHunt(1, " ", "c", gps, Guid.NewGuid(), 10);
        var actCode = () => MissionNode.CreateTreasureHunt(1, "i", "", gps, Guid.NewGuid(), 10);
        var actGps = () => MissionNode.CreateTreasureHunt(1, "i", "c", null!, Guid.NewGuid(), 10);
        var actScore = () => MissionNode.CreateTreasureHunt(1, "i", "c", gps, Guid.NewGuid(), 0);

        actParent.Should().Throw<ArgumentException>();
        actInst.Should().Throw<ArgumentException>();
        actCode.Should().Throw<ArgumentException>();
        actGps.Should().Throw<ArgumentNullException>();
        actScore.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateTrivia_WhenInvalid_Throws()
    {
        var actEmptyQs = () => MissionNode.CreateTrivia(
            1, Array.Empty<TriviaQuestion>(), Guid.NewGuid(), 10);
        var actNullQs = () => MissionNode.CreateTrivia(1, null!, Guid.NewGuid(), 10);
        var actParent = () => MissionNode.CreateTrivia(
            1, [new TriviaQuestion("Q", ["A", "B"], 0)], Guid.Empty, 10);
        var actScore = () => MissionNode.CreateTrivia(
            1, [new TriviaQuestion("Q", ["A", "B"], 0)], Guid.NewGuid(), 0);

        actEmptyQs.Should().Throw<ArgumentException>();
        actNullQs.Should().Throw<ArgumentException>();
        actParent.Should().Throw<ArgumentException>();
        actScore.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WhenInvalid_Throws()
    {
        var actTitle = () => MissionNode.Create("", "D", MissionNodeType.Stage, 1, 0);
        var actDesc = () => MissionNode.Create("T", "", MissionNodeType.Stage, 1, 0);
        var actScore = () => MissionNode.Create("T", "D", MissionNodeType.Stage, 1, -1);

        actTitle.Should().Throw<ArgumentException>();
        actDesc.Should().Throw<ArgumentException>();
        actScore.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ContainsNode_FindsSelfAndChildren()
    {
        var stage = MissionNode.Create("E", "D", MissionNodeType.Stage, 1, 0);
        stage.ContainsNode(stage.Id).Should().BeTrue();
        stage.ContainsNode(Guid.NewGuid()).Should().BeFalse();

        var child = MissionNode.CreateTrivia(
            1, [new TriviaQuestion("Q", ["A", "B"], 0)], stage.Id, 100);
        // AddChild is internal via Mission; use reflection or Mission aggregate:
        var mission = MissionManagement.Domain.Aggregates.Mission.Create("M", "D", DifficultyLevel.Medium);
        mission.AddRootNode(stage);
        var triviaId = mission.AddTriviaNode(
            stage.Id, [new TriviaQuestion("Q", ["A", "B"], 0)], 1, 100);
        stage.ContainsNode(triviaId).Should().BeTrue();
    }

    [Fact]
    public void HintCreate_WhenInvalid_Throws()
    {
        var actNode = () => Hint.Create(Guid.Empty, 1, "H", 0);
        var actOrder = () => Hint.Create(Guid.NewGuid(), 0, "H", 0);
        var actContent = () => Hint.Create(Guid.NewGuid(), 1, " ", 0);
        var actPenalty = () => Hint.Create(Guid.NewGuid(), 1, "H", -1);

        actNode.Should().Throw<ArgumentException>();
        actOrder.Should().Throw<ArgumentOutOfRangeException>();
        actContent.Should().Throw<ArgumentException>();
        actPenalty.Should().Throw<ArgumentOutOfRangeException>();
    }
}
