using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MissionManagement.Application.Common.Interfaces;

namespace MissionManagement.Infrastructure.External.UserService;

public sealed class HttpOperatorValidationService : IOperatorValidationService
{
    private readonly HttpClient _httpClient;

    public HttpOperatorValidationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsActiveOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IsActiveOperatorResponse>(
            $"api/v1/operators/{operatorId}/validation/is-active",
            cancellationToken);

        return response?.IsActive ?? false;
    }

    private sealed record IsActiveOperatorResponse(
        [property: JsonPropertyName("isActive")] bool IsActive);
}
