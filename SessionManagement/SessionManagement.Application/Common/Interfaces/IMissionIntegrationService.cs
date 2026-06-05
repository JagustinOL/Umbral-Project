namespace SessionManagement.Application.Common.Interfaces;

public interface IMissionIntegrationService
{
    Task<IReadOnlyList<AssignedMissionData>> GetAssignedMissionsForOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionNodeValidationData>> GetNodeValidationDataAsync(
        Guid missionId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetMissionDifficultyMultiplierAsync(
        Guid missionId,
        CancellationToken cancellationToken = default);

    Task<string?> GetMissionStatusAsync(
        Guid missionId,
        CancellationToken cancellationToken = default);
}

