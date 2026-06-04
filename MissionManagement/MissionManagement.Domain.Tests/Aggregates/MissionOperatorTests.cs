using FluentAssertions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Aggregates;

public sealed class MissionOperatorTests
{
    [Fact]
    public void AssignOperator_WhenDraftWithNodes_ActivatesMission()
    {
        var mission = Mission.Create("M", "D", DifficultyLevel.Easy);
        var stage = MissionNode.Create("E", "D", MissionNodeType.Stage, 1, 10);
        mission.AddRootNode(stage);
        var opId = Guid.NewGuid();

        mission.AssignOperator(opId);

        mission.Operators.Should().ContainSingle(o => o.OperatorId == opId);
        mission.Status.Should().Be(MissionStatus.Active);
    }

    [Fact]
    public void AssignOperator_WhenDuplicate_Throws()
    {
        var mission = Mission.Create("M", "D", DifficultyLevel.Easy);
        mission.AddRootNode(MissionNode.Create("E", "D", MissionNodeType.Stage, 1, 10));
        var opId = Guid.NewGuid();
        mission.AssignOperator(opId);
        var act = () => mission.AssignOperator(opId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeOperator_WhenAssigned_ReturnsToDraft()
    {
        var mission = Mission.Create("M", "D", DifficultyLevel.Easy);
        mission.AddRootNode(MissionNode.Create("E", "D", MissionNodeType.Stage, 1, 10));
        var opId = Guid.NewGuid();
        mission.AssignOperator(opId);
        mission.RevokeOperator(opId);
        mission.Operators.Should().BeEmpty();
        mission.Status.Should().Be(MissionStatus.Draft);
    }

    [Fact]
    public void Activate_WhenNoNodes_Throws()
    {
        var mission = Mission.Create("M", "D", DifficultyLevel.Easy);
        var act = () => mission.Activate();
        act.Should().Throw<InvalidOperationException>();
    }
}
