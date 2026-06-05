using FluentAssertions;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Aggregates;

public sealed class MissionMutationTests
{
    private static Mission BuildDraftWithTrivia(out MissionNode stage, out Guid triviaId)
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        stage = MissionNode.Create("Etapa", "Desc", MissionNodeType.Stage, 1, 10);
        mission.AddRootNode(stage);
        triviaId = mission.AddTriviaNode(stage.Id, [new TriviaQuestion("Q?", ["A", "B"], 0)], 1);
        return mission;
    }

    [Fact]
    public void UpdateNode_WhenDraft_UpdatesTitle()
    {
        var mission = BuildDraftWithTrivia(out var stage, out _);
        mission.UpdateNode(stage.Id, "Nuevo", "Desc nueva");
        stage.Title.Should().Be("Nuevo");
    }

    [Fact]
    public void UpdateTriviaNode_WhenDraft_UpdatesQuestions()
    {
        var mission = BuildDraftWithTrivia(out var stage, out var triviaId);
        mission.UpdateTriviaNode(triviaId, new List<TriviaQuestion> { new("Nueva", ["X", "Y"], 1) });
        mission.FindNodeById(triviaId)!.TriviaQuestions[0].Prompt.Should().Be("Nueva");
    }

    [Fact]
    public void UpdateTreasureHuntNode_WhenDraft_UpdatesSecretCode()
    {
        var mission = BuildDraftWithTrivia(out var stage, out _);
        var thId = mission.AddTreasureHuntNode(stage.Id, "Inst", "OLD", new GpsCoordinate(1, 2), 2);
        mission.UpdateTreasureHuntNode(thId, "Nueva inst", "NEW", new GpsCoordinate(3, 4));
        mission.FindNodeById(thId)!.SecretCode.Should().Be("NEW");
    }

    [Fact]
    public void DeleteNode_WhenDraft_RemovesNode()
    {
        var mission = BuildDraftWithTrivia(out var stage, out var triviaId);
        mission.DeleteNode(triviaId);
        mission.FindNodeById(triviaId).Should().BeNull();
    }

    [Fact]
    public void UpdateHint_DeleteHint_WhenDraft_ManagesHints()
    {
        var mission = BuildDraftWithTrivia(out _, out var triviaId);
        var hint = Hint.Create(triviaId, 1, "Pista", 5);
        mission.AddHintToNode(triviaId, hint);
        mission.UpdateHint(triviaId, hint.Id, "Actualizada");
        mission.FindNodeById(triviaId)!.Hints.Single().Content.Should().Be("Actualizada");
        mission.DeleteHint(triviaId, hint.Id);
        mission.FindNodeById(triviaId)!.Hints.Should().BeEmpty();
    }

    [Fact]
    public void Deactivate_WhenActive_SetsInactive()
    {
        var mission = BuildDraftWithTrivia(out var stage, out _);
        mission.Activate();
        mission.Deactivate();
        mission.Status.Should().Be(MissionStatus.Inactive);
    }
}
