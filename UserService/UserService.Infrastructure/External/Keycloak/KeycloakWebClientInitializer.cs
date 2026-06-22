using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UserService.Infrastructure.External.Keycloak;

public sealed class KeycloakWebClientInitializer
{
    private static readonly string[] RedirectUris =
    [
        "http://localhost:3000/*",
        "http://localhost:3001/*",
        "http://localhost:3002/*"
    ];

    private static readonly string[] WebOrigins =
    [
        "http://localhost:3000",
        "http://localhost:3001",
        "http://localhost:3002"
    ];

    public static async Task EnsureConfiguredAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 30;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await EnsureConfiguredInternalAsync(httpClient, options, logger, cancellationToken);
                logger.LogInformation("Keycloak OIDC client '{ClientId}' is ready.", options.WebClientId);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(
                    ex,
                    "Keycloak bootstrap attempt {Attempt}/{MaxAttempts} failed. Retrying in 3s…",
                    attempt,
                    maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"No fue posible configurar el cliente OIDC '{options.WebClientId}' en Keycloak tras {maxAttempts} intentos.");
    }

    private static async Task EnsureConfiguredInternalAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var adminToken = await GetAdminAccessTokenAsync(httpClient, options, cancellationToken);
        await EnsureRealmRoleAsync(httpClient, options, adminToken, options.AdminRole, cancellationToken);
        await KeycloakOperatorProfileInitializer.EnsureConfiguredAsync(
            httpClient,
            options,
            adminToken,
            logger,
            cancellationToken);
        await EnsureWebClientAsync(httpClient, options, adminToken, logger, cancellationToken);
        await EnsureDefaultAdminUserAsync(httpClient, options, adminToken, logger, cancellationToken);
    }

    private static async Task EnsureDefaultAdminUserAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        string adminToken,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.DefaultAdminEmail)
            || string.IsNullOrWhiteSpace(options.DefaultAdminPassword))
        {
            return;
        }

        var email = options.DefaultAdminEmail.Trim();
        var encodedEmail = Uri.EscapeDataString(email);

        using var searchRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl(options)}/users?email={encodedEmail}&exact=true");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var searchResponse = await httpClient.SendAsync(searchRequest, cancellationToken);
        searchResponse.EnsureSuccessStatusCode();

        var users = await searchResponse.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken)
            ?? [];

        string userId;
        if (users.Count == 0)
        {
            var userPayload = new CreateUserRequest(
                Username: email,
                Email: email,
                FirstName: options.DefaultAdminFirstName,
                LastName: options.DefaultAdminLastName,
                Enabled: true,
                EmailVerified: true);

            using var createRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{GetAdminRealmUrl(options)}/users")
            {
                Content = JsonContent.Create(userPayload)
            };
            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            using var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
            createResponse.EnsureSuccessStatusCode();

            var location = createResponse.Headers.Location?.ToString()
                ?? throw new InvalidOperationException("Keycloak no devolvió la cabecera Location al crear el admin.");

            userId = location.TrimEnd('/').Split('/').Last();
            logger.LogInformation("Created default admin user '{Email}'.", email);
        }
        else
        {
            userId = users[0].Id ?? throw new InvalidOperationException($"Keycloak devolvió un id inválido para '{email}'.");
            logger.LogInformation("Default admin user '{Email}' already exists.", email);
        }

        await AssignRealmRoleAsync(httpClient, options, adminToken, userId, options.AdminRole, cancellationToken);
        await KeycloakPasswordHelper.SetUserPasswordAsync(
            httpClient,
            GetAdminRealmUrl(options),
            adminToken,
            userId,
            options.DefaultAdminPassword,
            cancellationToken);
    }

    private static async Task AssignRealmRoleAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        string adminToken,
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var encodedRoleName = Uri.EscapeDataString(roleName);
        using var getRoleRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl(options)}/roles/{encodedRoleName}");
        getRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var getRoleResponse = await httpClient.SendAsync(getRoleRequest, cancellationToken);
        getRoleResponse.EnsureSuccessStatusCode();

        var roleRepresentation = await getRoleResponse.Content.ReadFromJsonAsync<KeycloakRoleRepresentation>(cancellationToken)
            ?? throw new InvalidOperationException($"Keycloak devolvió una representación inválida para el rol '{roleName}'.");

        var roleMappingsPayload = new[]
        {
            new RoleMappingRequest(roleRepresentation.Id, roleRepresentation.Name, roleRepresentation.Description)
        };

        using var assignRoleRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetAdminRealmUrl(options)}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(roleMappingsPayload)
        };
        assignRoleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var assignRoleResponse = await httpClient.SendAsync(assignRoleRequest, cancellationToken);
        assignRoleResponse.EnsureSuccessStatusCode();
    }

    private static async Task EnsureWebClientAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        string adminToken,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var encodedClientId = Uri.EscapeDataString(options.WebClientId);
        using var searchRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl(options)}/clients?clientId={encodedClientId}");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var searchResponse = await httpClient.SendAsync(searchRequest, cancellationToken);
        searchResponse.EnsureSuccessStatusCode();

        var clients = await searchResponse.Content.ReadFromJsonAsync<List<KeycloakClientRepresentation>>(cancellationToken)
            ?? [];

        var clientPayload = new KeycloakClientRepresentation
        {
            ClientId = options.WebClientId,
            Name = "UMBRAL Web Applications",
            Enabled = true,
            PublicClient = true,
            DirectAccessGrantsEnabled = true,
            StandardFlowEnabled = true,
            ImplicitFlowEnabled = false,
            Protocol = "openid-connect",
            RedirectUris = RedirectUris.ToList(),
            WebOrigins = WebOrigins.ToList()
        };

        if (clients.Count == 0)
        {
            using var createRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{GetAdminRealmUrl(options)}/clients")
            {
                Content = JsonContent.Create(clientPayload)
            };
            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            using var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
            createResponse.EnsureSuccessStatusCode();
            logger.LogInformation("Created Keycloak client '{ClientId}'.", options.WebClientId);
            return;
        }

        var existingClient = clients[0];
        if (string.IsNullOrWhiteSpace(existingClient.Id))
            throw new InvalidOperationException($"Keycloak devolvió un id inválido para el cliente '{options.WebClientId}'.");

        clientPayload.Id = existingClient.Id;

        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GetAdminRealmUrl(options)}/clients/{existingClient.Id}")
        {
            Content = JsonContent.Create(clientPayload)
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var updateResponse = await httpClient.SendAsync(updateRequest, cancellationToken);
        updateResponse.EnsureSuccessStatusCode();
        logger.LogInformation("Updated Keycloak client '{ClientId}'.", options.WebClientId);
    }

    private static async Task EnsureRealmRoleAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        string adminToken,
        string roleName,
        CancellationToken cancellationToken)
    {
        var encodedRoleName = Uri.EscapeDataString(roleName);
        using var getRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl(options)}/roles/{encodedRoleName}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var getResponse = await httpClient.SendAsync(getRequest, cancellationToken);
        if (getResponse.IsSuccessStatusCode)
            return;

        if (getResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
            getResponse.EnsureSuccessStatusCode();

        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetAdminRealmUrl(options)}/roles")
        {
            Content = JsonContent.Create(new CreateRoleRequest(roleName, $"UMBRAL {roleName} role"))
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var createResponse = await httpClient.SendAsync(createRequest, cancellationToken);
        createResponse.EnsureSuccessStatusCode();
    }

    private static async Task<string> GetAdminAccessTokenAsync(
        HttpClient httpClient,
        KeycloakOptions options,
        CancellationToken cancellationToken)
    {
        var formValues = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = options.AdminClientId,
            ["username"] = options.AdminUsername,
            ["password"] = options.AdminPassword
        };

        if (!string.IsNullOrWhiteSpace(options.AdminClientSecret))
            formValues["client_secret"] = options.AdminClientSecret;

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.BaseUrl.TrimEnd('/')}/realms/{options.AdminRealm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        using var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Keycloak no devolvió un token administrativo válido.");

        if (string.IsNullOrWhiteSpace(tokenBody.AccessToken))
            throw new InvalidOperationException("Keycloak no devolvió un token administrativo válido.");

        return tokenBody.AccessToken;
    }

    private static string GetAdminRealmUrl(KeycloakOptions options)
        => $"{options.BaseUrl.TrimEnd('/')}/admin/realms/{options.Realm}";

    private sealed record CreateRoleRequest(string Name, string Description);

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class KeycloakClientRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("publicClient")]
        public bool PublicClient { get; set; }

        [JsonPropertyName("directAccessGrantsEnabled")]
        public bool DirectAccessGrantsEnabled { get; set; }

        [JsonPropertyName("standardFlowEnabled")]
        public bool StandardFlowEnabled { get; set; }

        [JsonPropertyName("implicitFlowEnabled")]
        public bool ImplicitFlowEnabled { get; set; }

        [JsonPropertyName("protocol")]
        public string Protocol { get; set; } = "openid-connect";

        [JsonPropertyName("redirectUris")]
        public List<string> RedirectUris { get; set; } = [];

        [JsonPropertyName("webOrigins")]
        public List<string> WebOrigins { get; set; } = [];
    }

    private sealed record CreateUserRequest(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        bool EmailVerified);

    private sealed record RoleMappingRequest(string Id, string Name, string? Description);

    private sealed class KeycloakUserRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }
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
}
