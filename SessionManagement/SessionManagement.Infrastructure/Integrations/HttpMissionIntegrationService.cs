using System.Net.Http.Json;
using System.Text.Json;
using SessionManagement.Application.Common.Interfaces;

namespace SessionManagement.Infrastructure.Integrations;

public sealed class HttpMissionIntegrationService : IMissionIntegrationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            JsonOptions,
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

    public async Task<decimal> GetMissionDifficultyMultiplierAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        var mission = await _httpClient.GetFromJsonAsync<MissionDetail>(
            $"api/v1/missions/{missionId}",
            JsonOptions,
            cancellationToken);

        if (mission is null)
            throw new InvalidOperationException($"No se pudo obtener la misión con Id={missionId} desde MissionManagement.");

        return mission.DifficultyScoreMultiplier;
    }

    public async Task<string?> GetMissionStatusAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        var mission = await _httpClient.GetFromJsonAsync<MissionDetail>(
            $"api/v1/missions/{missionId}",
            JsonOptions,
            cancellationToken);

        return mission?.Status;
    }

    public async Task<IReadOnlyList<MissionHintData>> GetHintsForNodeAsync(
        Guid missionId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var hints = await _httpClient.GetFromJsonAsync<IReadOnlyList<MissionHintResponse>>(
            $"api/v1/missions/{missionId}/nodes/{nodeId}/hints",
            JsonOptions,
            cancellationToken) ?? [];

        return hints
            .Select(h => new MissionHintData(h.Id, h.Order, h.Content, h.PenaltyPoints))
            .ToList();
    }

    private sealed record MissionHintResponse(
        Guid Id,
        int Order,
        string Content,
        int PenaltyPoints);

    private sealed record MissionSummary(
        Guid Id,
        string Title,
        IReadOnlyList<Guid> OperatorIds);

    private sealed record MissionDetail(
        Guid Id,
        string Title,
        string Description,
        string Status,
        string Difficulty,
        decimal DifficultyScoreMultiplier,
        int? MaxDurationMinutes,
        DateTime CreatedAtUtc,
        DateTime? LastModifiedAtUtc,
        IReadOnlyList<Guid> OperatorIds);

    private sealed record MissionNodeValidation(
        Guid NodeId,
        string NodeType,
        int ExecutionOrder,
        int BaseScore,
        string ExpectedValue);

    private async Task<IReadOnlyList<MissionNodeValidationData>> GetNodeValidationsInternalAsync(
        Guid missionId,
        CancellationToken cancellationToken)
    {
        var validations = await _httpClient.GetFromJsonAsync<IReadOnlyList<MissionNodeValidation>>(
            $"api/v1/missions/{missionId}/node-validations",
            JsonOptions,
            cancellationToken) ?? [];

        return validations
            .Select(v => new MissionNodeValidationData(
                NodeId: v.NodeId,
                NodeType: v.NodeType,
                ExecutionOrder: v.ExecutionOrder,
                BaseScore: v.BaseScore,
                ExpectedValue: v.ExpectedValue))
            .ToList();
    }
}

