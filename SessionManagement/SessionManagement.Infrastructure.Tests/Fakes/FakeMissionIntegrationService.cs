using SessionManagement.Application.Common.Interfaces;

namespace SessionManagement.Infrastructure.Tests.Fakes;

public sealed class FakeMissionIntegrationService : IMissionIntegrationService
{
    public Task<IReadOnlyList<AssignedMissionData>> GetAssignedMissionsForOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AssignedMissionData> result =
        [
            new(
                MissionId: Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
                OperatorId: operatorId,
                Title: "Mision Operador Alpha"),
            new(
                MissionId: Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
                OperatorId: operatorId,
                Title: "Mision Operador Beta")
        ];

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<MissionNodeValidationData>> GetNodeValidationDataAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MissionNodeValidationData> result =
        [
            new(
                NodeId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                NodeType: "Trivia",
                ExecutionOrder: 1,
                BaseScore: 100,
                ExpectedAnswers: ["Bogota"]),
            new(
                NodeId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                NodeType: "TreasureHunt",
                ExecutionOrder: 2,
                BaseScore: 150,
                ExpectedAnswers: ["CODE-123"])
        ];

        return Task.FromResult(result);
    }

    public Task<decimal> GetMissionDifficultyMultiplierAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(1.5m);

    public Task<string?> GetMissionStatusAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<string?>("Active");

    public Task<IReadOnlyList<MissionHintData>> GetHintsForNodeAsync(
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<MissionHintData>>([]);

    public Task<PlayerNodeContentData> GetNodePlayerContentAsync(
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PlayerNodeContentData(
            nodeId,
            "Trivia",
            [new PlayerTriviaQuestionData("Sample question", ["A", "B"])],
            null));
}
