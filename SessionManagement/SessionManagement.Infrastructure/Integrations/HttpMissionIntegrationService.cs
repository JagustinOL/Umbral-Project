using System.Net.Http.Json;
using SessionManagement.Application.Common.Interfaces;

namespace SessionManagement.Infrastructure.Integrations;

public sealed class HttpMissionIntegrationService : IMissionIntegrationService
{
    private readonly HttpClient _httpClient;

    public HttpMissionIntegrationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<AssignedMissionData>> GetAssignedMissionsForOperatorAsync(
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        var missions = await _httpClient.GetFromJsonAsync<IReadOnlyList<MissionSummary>>(
            "api/v1/missions",
            cancellationToken) ?? [];

        return missions
            .Where(m => m.OperatorIds?.Contains(operatorId) == true)
            .Select(m => new AssignedMissionData(
                MissionId: m.Id,
                OperatorId: operatorId,
                Title: m.Title))
            .ToList();
    }

    public Task<IReadOnlyList<MissionNodeValidationData>> GetNodeValidationDataAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        return GetNodeValidationsInternalAsync(missionId, cancellationToken);
    }

    private sealed record MissionSummary(
        Guid Id,
        string Title,
        IReadOnlyList<Guid> OperatorIds);

    private sealed record MissionNodeValidation(
        Guid NodeId,
        string NodeType,
        int ExecutionOrder,
        string ExpectedValue);

    private async Task<IReadOnlyList<MissionNodeValidationData>> GetNodeValidationsInternalAsync(
        Guid missionId,
        CancellationToken cancellationToken)
    {
        var validations = await _httpClient.GetFromJsonAsync<IReadOnlyList<MissionNodeValidation>>(
            $"api/v1/missions/{missionId}/node-validations",
            cancellationToken) ?? [];

        return validations
            .Select(v => new MissionNodeValidationData(
                NodeId: v.NodeId,
                NodeType: v.NodeType,
                ExecutionOrder: v.ExecutionOrder,
                ExpectedValue: v.ExpectedValue))
            .ToList();
    }
}

