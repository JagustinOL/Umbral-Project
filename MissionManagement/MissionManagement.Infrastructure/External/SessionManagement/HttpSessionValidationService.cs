using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Infrastructure.External.SessionManagement;

public sealed class HttpSessionValidationService : ISessionValidationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpSessionValidationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> HasActiveSessionsAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<HasActiveSessionsResponse>(
            $"api/v1/operators/{operatorId}/session-validation/has-active",
            JsonOptions,
            cancellationToken);

        return response?.HasActiveSessions ?? false;
    }

    public async Task<bool> IsSupervisingMissionAsync(
        Guid operatorId,
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IsSupervisingMissionResponse>(
            $"api/v1/operators/{operatorId}/missions/{missionId}/session-validation/is-supervising",
            JsonOptions,
            cancellationToken);

        return response?.IsSupervising ?? false;
    }

    public async Task<bool> HasOpenSessionsForMissionAsync(
        Guid missionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<HasOpenSessionsForMissionResponse>(
            $"api/v1/missions/{missionId}/session-validation/has-open",
            JsonOptions,
            cancellationToken);

        return response?.HasOpenSessions ?? false;
    }

    private sealed record HasActiveSessionsResponse(
        [property: JsonPropertyName("hasActiveSessions")] bool HasActiveSessions);

    private sealed record IsSupervisingMissionResponse(
        [property: JsonPropertyName("isSupervising")] bool IsSupervising);

    private sealed record HasOpenSessionsForMissionResponse(
        [property: JsonPropertyName("hasOpenSessions")] bool HasOpenSessions);
}
