using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MissionManagement.Infrastructure.External.Keycloak;

internal static class KeycloakOperatorProfileInitializer
{
    private static readonly string[] RequiredAttributes =
    [
        OperatorSetupCodeHelper.SetupCodeHashAttribute,
        OperatorSetupCodeHelper.SetupCodeExpiresAttribute
    ];

    public static async Task EnsureConfiguredAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        string adminToken,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        using var getRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl(options)}/users/profile");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var getResponse = await httpClient.SendAsync(getRequest, cancellationToken);
        getResponse.EnsureSuccessStatusCode();

        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileRepresentation>(cancellationToken)
            ?? throw new InvalidOperationException("Keycloak devolvió un perfil de usuario vacío.");

        var existing = profile.Attributes?.Select(attribute => attribute.Name).ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);

        var missing = RequiredAttributes.Where(name => !existing.Contains(name)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        profile.Attributes ??= [];
        foreach (var attributeName in missing)
        {
            profile.Attributes.Add(new UserProfileAttributeRepresentation
            {
                Name = attributeName,
                DisplayName = attributeName,
                Multivalued = false,
                Permissions = new UserProfilePermissionsRepresentation
                {
                    View = ["admin"],
                    Edit = ["admin"]
                }
            });
        }

        using var putRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GetAdminRealmUrl(options)}/users/profile")
        {
            Content = JsonContent.Create(profile)
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var putResponse = await httpClient.SendAsync(putRequest, cancellationToken);
        putResponse.EnsureSuccessStatusCode();

        logger.LogInformation(
            "Registered Keycloak user profile attributes for operator activation: {Attributes}",
            string.Join(", ", missing));
    }

    private static string GetAdminRealmUrl(KeycloakOptions options) =>
        $"{options.BaseUrl.TrimEnd('/')}/admin/realms/{options.Realm}";

    private sealed class UserProfileRepresentation
    {
        [JsonPropertyName("attributes")]
        public List<UserProfileAttributeRepresentation>? Attributes { get; set; }
    }

    private sealed class UserProfileAttributeRepresentation
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; init; } = string.Empty;

        [JsonPropertyName("permissions")]
        public UserProfilePermissionsRepresentation? Permissions { get; set; }

        [JsonPropertyName("multivalued")]
        public bool Multivalued { get; init; }
    }

    private sealed class UserProfilePermissionsRepresentation
    {
        [JsonPropertyName("view")]
        public List<string> View { get; init; } = [];

        [JsonPropertyName("edit")]
        public List<string> Edit { get; init; } = [];
    }
}
