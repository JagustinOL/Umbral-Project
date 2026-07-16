using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Application.Tests.Support;

internal static class MissionTestData
{
    public static Mission CreateMissionWithStage(out MissionNode stage)
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        return mission;
    }

    public static Mission CreateMissionWithTriviaGame(out MissionNode triviaNode)
    {
        var mission = CreateMissionWithStage(out var stage);
        var triviaId = mission.AddTriviaNode(
            parentNodeId: stage.Id,
            questions: [new TriviaQuestion("¿Pregunta?", ["A", "B"], correctOptionIndex: 0)],
            executionOrder: 1,
            baseScore: 100);
        triviaNode = mission.FindNodeById(triviaId)!;
        return mission;
    }

    public static Mission CreateMissionWithTreasureHuntGame(out MissionNode treasureNode)
    {
        var mission = CreateMissionWithStage(out var stage);
        var nodeId = mission.AddTreasureHuntNode(
            parentNodeId: stage.Id,
            instructions: "Busca el código",
            secretCode: "ABC123",
            destination: new GpsCoordinate(4.711, -74.0721),
            executionOrder: 1,
            baseScore: 100);
        treasureNode = mission.FindNodeById(nodeId)!;
        return mission;
    }

    public static Mission CreateMissionWithHint(out MissionNode triviaNode, out Guid hintId)
    {
        var mission = CreateMissionWithTriviaGame(out triviaNode);
        var hint = Hint.Create(triviaNode.Id, order: 1, content: "Pista inicial", penaltyPoints: 5);
        mission.AddHintToNode(triviaNode.Id, hint);
        hintId = hint.Id;
        return mission;
    }
}
