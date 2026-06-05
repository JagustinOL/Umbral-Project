using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace MissionManagement.Infrastructure.External.Keycloak;

public sealed class KeycloakPlayerIdentityService : IPlayerIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakPlayerIdentityService(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Guid> CreatePlayerAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var userPayload = new CreateUserRequest(
            Username: email,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            Enabled: true,
            EmailVerified: true);

        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetAdminRealmUrl()}/users")
        {
            Content = JsonContent.Create(userPayload)
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
        if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new ConflictException($"Ya existe un jugador registrado con el correo '{email}'.");

        if (!createResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                "No fue posible crear la cuenta de jugador en Keycloak.",
                createResponse,
                cancellationToken));
        }

        var location = createResponse.Headers.Location?.ToString();
        var keycloakUserId = ExtractUserIdFromLocationHeader(location);
        if (!Guid.TryParse(keycloakUserId, out var playerId))
            throw new InvalidOperationException($"Keycloak devolvió un id de usuario no válido ('{keycloakUserId}').");

        await AssignRealmRoleAsync(accessToken, keycloakUserId, _options.PlayerRole, cancellationToken);
        await KeycloakPasswordHelper.SetUserPasswordAsync(
            _httpClient,
            GetAdminRealmUrl(),
            accessToken,
            keycloakUserId,
            password,
            cancellationToken);

        return playerId;
    }

    public async Task<IReadOnlyList<PlayerIdentityDto>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var roleName = Uri.EscapeDataString(_options.PlayerRole);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/roles/{roleName}/users?first=0&max=1000");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                "No fue posible consultar jugadores en Keycloak.",
                response,
                cancellationToken));
        }

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken)
            ?? [];

        return users
            .Where(user => Guid.TryParse(user.Id, out _))
            .Select(user => new PlayerIdentityDto(
                PlayerId: Guid.Parse(user.Id),
                FirstName: user.FirstName ?? string.Empty,
                LastName: user.LastName ?? string.Empty,
                Email: user.Email ?? string.Empty,
                IsActive: user.Enabled))
            .ToList();
    }

    public async Task<PlayerIdentityDto> GetPlayerByIdAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/users/{playerId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException($"No existe un jugador con Id={playerId}.");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                $"No fue posible consultar el jugador con Id={playerId}.",
                response,
                cancellationToken));
        }

        var user = await response.Content.ReadFromJsonAsync<KeycloakUserRepresentation>(cancellationToken);
        if (user is null || !Guid.TryParse(user.Id, out _))
            throw new InvalidOperationException($"Keycloak devolvió una representación inválida para jugador Id={playerId}.");

        return new PlayerIdentityDto(
            PlayerId: Guid.Parse(user.Id),
            FirstName: user.FirstName ?? string.Empty,
            LastName: user.LastName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            IsActive: user.Enabled);
    }

    public async Task UpdatePlayerAsync(
        Guid playerId,
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetPlayerByIdAsync(playerId, cancellationToken);
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var payload = new UpdateUserRequest(
            FirstName: firstName,
            LastName: lastName,
            Email: email,
            Enabled: existing.IsActive);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GetAdminRealmUrl()}/users/{playerId}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException($"No existe un jugador con Id={playerId}.");
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new ConflictException($"Ya existe un jugador registrado con el correo '{email}'.");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                $"No fue posible actualizar el jugador con Id={playerId} en Keycloak.",
                response,
                cancellationToken));
        }
    }

    public async Task DeactivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var payload = new UpdateUserEnabledRequest(Enabled: false);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GetAdminRealmUrl()}/users/{playerId}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new NotFoundException($"No existe un jugador con Id={playerId}.");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                $"No fue posible desactivar el jugador con Id={playerId} en Keycloak.",
                response,
                cancellationToken));
        }
    }

    private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
    {
        var formValues = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.AdminClientId,
            ["username"] = _options.AdminUsername,
            ["password"] = _options.AdminPassword
        };

        if (!string.IsNullOrWhiteSpace(_options.AdminClientSecret))
            formValues["client_secret"] = _options.AdminClientSecret;

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetBaseUrl()}/realms/{_options.AdminRealm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                "No fue posible obtener token administrativo de Keycloak.",
                tokenResponse,
                cancellationToken));
        }

        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokenBody is null || string.IsNullOrWhiteSpace(tokenBody.AccessToken))
            throw new InvalidOperationException("Keycloak no devolvió un token administrativo válido.");

        return tokenBody.AccessToken;
    }

    private async Task AssignRealmRoleAsync(
        string accessToken,
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var encodedRoleName = Uri.EscapeDataString(roleName);
        using var getRoleRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/roles/{encodedRoleName}");
        getRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var getRoleResponse = await _httpClient.SendAsync(getRoleRequest, cancellationToken);
        if (!getRoleResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                $"No fue posible recuperar el rol '{roleName}' en Keycloak.",
                getRoleResponse,
                cancellationToken));
        }

        var roleRepresentation = await getRoleResponse.Content.ReadFromJsonAsync<KeycloakRoleRepresentation>(cancellationToken);
        if (roleRepresentation is null || string.IsNullOrWhiteSpace(roleRepresentation.Id))
        {
            throw new InvalidOperationException(
                $"Keycloak devolvió una representación inválida para el rol '{roleName}'.");
        }

        var roleMappingsPayload = new[]
        {
            new RoleMappingRequest(
                Id: roleRepresentation.Id,
                Name: roleRepresentation.Name,
                Description: roleRepresentation.Description)
        };

        using var assignRoleRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetAdminRealmUrl()}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(roleMappingsPayload)
        };
        assignRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var assignRoleResponse = await _httpClient.SendAsync(assignRoleRequest, cancellationToken);
        if (!assignRoleResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await BuildKeycloakErrorAsync(
                $"No fue posible asignar el rol '{roleName}' al jugador creado.",
                assignRoleResponse,
                cancellationToken));
        }
    }

    private static string ExtractUserIdFromLocationHeader(string? locationHeader)
    {
        if (string.IsNullOrWhiteSpace(locationHeader))
            throw new InvalidOperationException("Keycloak no devolvió la cabecera Location al crear el jugador.");

        return locationHeader.TrimEnd('/').Split('/').Last();
    }

    private static async Task<string> BuildKeycloakErrorAsync(
        string contextMessage,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseBody))
            return $"{contextMessage} StatusCode={(int)response.StatusCode}.";

        return $"{contextMessage} StatusCode={(int)response.StatusCode}. Body={responseBody}";
    }

    private string GetAdminRealmUrl()
    {
        return $"{GetBaseUrl()}/admin/realms/{_options.Realm}";
    }

    private string GetBaseUrl()
    {
        return _options.BaseUrl.TrimEnd('/');
    }

    private sealed record CreateUserRequest(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        bool EmailVerified);

    private sealed record UpdateUserRequest(
        string FirstName,
        string LastName,
        string Email,
        bool Enabled);

    private sealed record UpdateUserEnabledRequest(bool Enabled);

    private sealed record RoleMappingRequest(string Id, string Name, string? Description);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class KeycloakRoleRepresentation
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }

    private sealed class KeycloakUserRepresentation
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string? FirstName { get; init; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; }
    }
}
