using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MissionManagement.Infrastructure.External.Keycloak;

internal static class KeycloakPasswordHelper
{
    public static async Task SetUserPasswordAsync(
        HttpClient httpClient,
        string adminRealmUrl,
        string accessToken,
        string userId,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        var payload = new ResetPasswordRequest(
            Type: "password",
            Value: password,
            Temporary: false);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{adminRealmUrl}/users/{userId}/reset-password")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"No fue posible establecer la contraseña del usuario en Keycloak. StatusCode={(int)response.StatusCode}. Body={body}");
        }
    }

    private sealed record ResetPasswordRequest(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("temporary")] bool Temporary);
}
