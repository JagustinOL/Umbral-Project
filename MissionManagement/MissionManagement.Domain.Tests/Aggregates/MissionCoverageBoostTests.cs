using FluentAssertions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Events;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Aggregates;

public sealed class MissionCoverageBoostTests
{
    private static Mission BuildDraftWithGames(out MissionNode stage, out Guid triviaId, out Guid treasureId)
    {
        var mission = Mission.Create("Misión", "Descripción válida", DifficultyLevel.Medium, maxDurationMinutes: 60);
        stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 0);
        mission.AddRootNode(stage);
        triviaId = mission.AddTriviaNode(
            stage.Id,
            [new TriviaQuestion("¿Capital?", ["Bogotá", "Cali"], 0)],
            executionOrder: 1,
            baseScore: 100);
        treasureId = mission.AddTreasureHuntNode(
            stage.Id,
            "Busca el QR",
            "UMBRAL",
            new GpsCoordinate(4.711, -74.0721),
            executionOrder: 2,
            baseScore: 80);
        return mission;
    }

    [Fact]
    public void Create_WhenInvalidArgs_Throws()
    {
        var actTitle = () => Mission.Create("", "Desc", DifficultyLevel.Easy);
        var actDesc = () => Mission.Create("T", "", DifficultyLevel.Easy);
        var actDuration = () => Mission.Create("T", "D", DifficultyLevel.Easy, maxDurationMinutes: 0);

        actTitle.Should().Throw<ArgumentException>();
        actDesc.Should().Throw<ArgumentException>();
        actDuration.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_WhenDraft_UpdatesFields()
    {
        var mission = Mission.Create("Old", "Old desc", DifficultyLevel.Easy);
        mission.UpdateDetails("Nuevo", "Nueva desc", 45);
        mission.Title.Should().Be("Nuevo");
        mission.Description.Should().Be("Nueva desc");
        mission.MaxDurationMinutes.Should().Be(45);
    }

    [Fact]
    public void UpdateDetails_WhenInvalid_Throws()
    {
        var mission = Mission.Create("T", "D", DifficultyLevel.Easy);
        var actTitle = () => mission.UpdateDetails("", "D", null);
        var actDesc = () => mission.UpdateDetails("T", " ", null);
        var actDur = () => mission.UpdateDetails("T", "D", 0);

        actTitle.Should().Throw<ArgumentException>();
        actDesc.Should().Throw<ArgumentException>();
        actDur.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DeleteNode_WhenRootStage_RemovesEntireTree()
    {
        var mission = BuildDraftWithGames(out var stage, out var triviaId, out _);
        mission.DeleteNode(stage.Id);
        mission.Nodes.Should().BeEmpty();
        mission.FindNodeById(triviaId).Should().BeNull();
    }

    [Fact]
    public void GetAllowedNodeIds_ReturnsOnlyLeafGameNodes()
    {
        var mission = BuildDraftWithGames(out var stage, out var triviaId, out var treasureId);
        var ids = mission.GetAllowedNodeIds();
        ids.Should().BeEquivalentTo([triviaId, treasureId]);
        ids.Should().NotContain(stage.Id);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        mission.Activate();
        var act = () => mission.Activate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*ya está activa*");
    }

    [Fact]
    public void Activate_WhenInactive_Throws()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        mission.Activate();
        mission.Deactivate();
        var act = () => mission.Activate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*inactiva*");
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        mission.Activate();
        mission.Deactivate();
        var act = () => mission.Deactivate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Activate_PublishesActivatedNodeSnapshotsWithLeafNodes()
    {
        var mission = BuildDraftWithGames(out _, out var triviaId, out var treasureId);
        mission.Activate();
        var ev = mission.DomainEvents.OfType<MissionActivatedEvent>().Single();
        ev.AllowedNodes.Should().Contain(n => n.NodeId == triviaId && n.NodeType.Contains("Trivia"));
        ev.AllowedNodes.Should().Contain(n => n.NodeId == treasureId && n.NodeType.Contains("Treasure"));
        ev.AllowedNodes.Should().OnlyContain(n => n.BaseScore > 0);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesQueue()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        mission.Activate();
        mission.DomainEvents.Should().NotBeEmpty();
        mission.ClearDomainEvents();
        mission.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddRootNode_WhenParentSetOrNullOrActive_Throws()
    {
        var mission = Mission.Create("T", "D", DifficultyLevel.Easy);
        var orphan = MissionNode.Create("E", "D", MissionNodeType.Stage, 1, 0);
        typeof(MissionNode).GetProperty(nameof(MissionNode.ParentNodeId))!
            .SetValue(orphan, Guid.NewGuid());
        var actParent = () => mission.AddRootNode(orphan);
        actParent.Should().Throw<InvalidOperationException>();

        var actNull = () => mission.AddRootNode(null!);
        actNull.Should().Throw<ArgumentNullException>();

        var stage = MissionNode.Create("E", "D", MissionNodeType.Stage, 1, 0);
        mission.AddRootNode(stage);
        mission.Activate();
        var actActive = () => mission.AddRootNode(
            MissionNode.Create("E2", "D", MissionNodeType.Stage, 2, 0));
        actActive.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddTriviaNode_WhenParentMissingOrWrongType_Throws()
    {
        var mission = BuildDraftWithGames(out _, out var triviaId, out _);
        var actMissing = () => mission.AddTriviaNode(
            Guid.NewGuid(), [new TriviaQuestion("Q", ["A", "B"], 0)], 9, 50);
        actMissing.Should().Throw<InvalidOperationException>();

        var actWrongParent = () => mission.AddTriviaNode(
            triviaId, [new TriviaQuestion("Q", ["A", "B"], 0)], 9, 50);
        actWrongParent.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddTriviaNode_WhenSiblingOrderConflicts_Throws()
    {
        var mission = BuildDraftWithGames(out var stage, out _, out _);
        var act = () => mission.AddTriviaNode(
            stage.Id, [new TriviaQuestion("Q2", ["A", "B"], 0)], executionOrder: 1, baseScore: 50);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateNode_WhenInvalid_Throws()
    {
        var mission = BuildDraftWithGames(out _, out var triviaId, out _);
        var actEmpty = () => mission.UpdateNode(Guid.Empty, "T", "D");
        var actMissing = () => mission.UpdateNode(Guid.NewGuid(), "T", "D");
        var actNotStage = () => mission.UpdateNode(triviaId, "T", "D");

        actEmpty.Should().Throw<ArgumentException>();
        actMissing.Should().Throw<InvalidOperationException>();
        actNotStage.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeleteNode_WhenInvalid_Throws()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        var actEmpty = () => mission.DeleteNode(Guid.Empty);
        var actMissing = () => mission.DeleteNode(Guid.NewGuid());
        actEmpty.Should().Throw<ArgumentException>();
        actMissing.Should().Throw<InvalidOperationException>();

        mission.Activate();
        var actActive = () => mission.DeleteNode(mission.Nodes[0].Id);
        actActive.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void HintOps_WhenInvalid_Throw()
    {
        var mission = BuildDraftWithGames(out var stage, out var triviaId, out _);
        var actNull = () => mission.AddHintToNode(triviaId, null!);
        actNull.Should().Throw<ArgumentNullException>();

        var actMissingNode = () => mission.AddHintToNode(Guid.NewGuid(), Hint.Create(Guid.NewGuid(), 1, "H", 1));
        actMissingNode.Should().Throw<InvalidOperationException>();

        var hint = Hint.Create(triviaId, 1, "Pista", 5);
        mission.AddHintToNode(triviaId, hint);

        var actUpdateMissing = () => mission.UpdateHint(triviaId, Guid.NewGuid(), "X");
        actUpdateMissing.Should().Throw<InvalidOperationException>();

        var actUpdateStage = () => mission.UpdateHint(stage.Id, hint.Id, "X");
        actUpdateStage.Should().Throw<InvalidOperationException>();

        var actDeleteMissing = () => mission.DeleteHint(triviaId, Guid.NewGuid());
        actDeleteMissing.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeOperator_WhenTwoOperators_KeepsActive()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        var op1 = Guid.NewGuid();
        var op2 = Guid.NewGuid();
        mission.AssignOperator(op1);
        mission.AssignOperator(op2);
        mission.Status.Should().Be(MissionStatus.Active);

        mission.RevokeOperator(op1);
        mission.Status.Should().Be(MissionStatus.Active);
        mission.Operators.Should().ContainSingle(o => o.OperatorId == op2);
    }

    [Fact]
    public void RevokeOperator_WhenEmpty_Throws()
    {
        var mission = BuildDraftWithGames(out _, out _, out _);
        var act = () => mission.RevokeOperator(Guid.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
