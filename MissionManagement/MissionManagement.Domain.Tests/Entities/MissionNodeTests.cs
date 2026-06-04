using FluentAssertions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Entities;

public sealed class MissionNodeTests
{
    [Fact]
    public void Create_WhenInvalidExecutionOrder_Throws()
    {
        var act = () => MissionNode.Create("T", "D", MissionNodeType.Stage, 0, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CreateTrivia_WithQuestions_SetsProperties()
    {
        var node = MissionNode.CreateTrivia(1, [new TriviaQuestion("Q", ["A", "B"], 0)], Guid.NewGuid(), 50);
        node.NodeType.Should().Be(MissionNodeType.Trivia);
        node.TriviaQuestions.Should().HaveCount(1);
    }
}
