using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using UserService.Application.Common;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;
using Microsoft.Extensions.Options;
using System.Net;

namespace UserService.Infrastructure.External.Keycloak;

public sealed class KeycloakIdentityService : IIdentityService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakIdentityService(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<CreateOperatorResult> CreateOperatorAsync(
        string firstName,
        string lastName,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var setupCode = OperatorSetupCodeHelper.GenerateSetupCode();
            var setupCodeHash = OperatorSetupCodeHelper.HashSetupCode(setupCode, _options.OperatorSetupCodeSalt);
            var expiresUtc = OperatorSetupCodeHelper.ComputeExpiryUtc(_options.OperatorSetupCodeTtlDays);

            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var userPayload = new CreateUserRequest(
                Username: email,
                Email: email,
                FirstName: firstName,
                LastName: lastName,
                Enabled: false,
                EmailVerified: true,
                Attributes: new Dictionary<string, List<string>>
                {
                    [OperatorSetupCodeHelper.SetupCodeHashAttribute] = [setupCodeHash],
                    [OperatorSetupCodeHelper.SetupCodeExpiresAttribute] =
                        [expiresUtc.ToString("O")]
                });

            var userId = await CreateRealmUserAsync(
                accessToken,
                userPayload,
                $"Ya existe un operador registrado con el correo '{email}'.",
                "No fue posible crear la cuenta de operador en Keycloak.",
                cancellationToken);

            await PersistOperatorSetupAttributesAsync(
                accessToken,
                userId.ToString(),
                firstName,
                lastName,
                email,
                setupCodeHash,
                expiresUtc,
                cancellationToken);

            await AssignRealmRoleAsync(accessToken, userId.ToString(), _options.OperatorRole, cancellationToken);

            return new CreateOperatorResult(userId, setupCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalDependencyException(
                "No fue posible comunicarse con Keycloak durante la creación del operador.",
                ex);
        }
    }

    public async Task SetupOperatorPasswordAsync(
        string email,
        string setupCode,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var user = await FindUserByEmailAsync(accessToken, email, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Id))
                throw new NotFoundException("No se encontró una cuenta de operador pendiente de activación.");

            if (!await UserHasRealmRoleAsync(accessToken, user.Id, _options.OperatorRole, cancellationToken))
                throw new NotFoundException("No se encontró una cuenta de operador pendiente de activación.");

            if (user.Enabled)
                throw new ConflictException("La cuenta ya está activada. Inicie sesión con su contraseña.");

            if (await UserHasPasswordCredentialAsync(accessToken, user.Id, cancellationToken))
                throw new ConflictException(
                    "La cuenta fue desactivada por un administrador. Contacte al administrador para reactivarla.");

            var storedHash = GetAttributeValue(user, OperatorSetupCodeHelper.SetupCodeHashAttribute);
            var expiresRaw = GetAttributeValue(user, OperatorSetupCodeHelper.SetupCodeExpiresAttribute);

            if (OperatorSetupCodeHelper.IsExpired(expiresRaw))
                throw new ConflictException("El código de activación expiró. Solicite uno nuevo al administrador.");

            if (string.IsNullOrWhiteSpace(storedHash)
                || !OperatorSetupCodeHelper.CodesMatch(setupCode, storedHash, _options.OperatorSetupCodeSalt))
                throw new ConflictException("El código de activación no es válido.");

            await KeycloakPasswordHelper.SetUserPasswordAsync(
                _httpClient,
                GetAdminRealmUrl(),
                accessToken,
                user.Id,
                password,
                cancellationToken);

            await UpdateUserActivationAsync(
                accessToken,
                user.Id,
                enabled: true,
                clearSetupAttributes: true,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalDependencyException(
                "No fue posible comunicarse con Keycloak durante la activación del operador.",
                ex);
        }
    }

    public async Task<Guid> CreateAdminAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await CreateRealmUserAsync(
                firstName,
                lastName,
                email,
                password,
                _options.AdminRole,
                $"Ya existe un administrador registrado con el correo '{email}'.",
                "No fue posible crear la cuenta de administrador en Keycloak.",
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalDependencyException(
                "No fue posible comunicarse con Keycloak durante la creación del administrador.",
                ex);
        }
    }

    private async Task<Guid> CreateRealmUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string realmRole,
        string conflictMessage,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var userPayload = new CreateUserRequest(
            Username: email,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            Enabled: true,
            EmailVerified: true);

        var userId = await CreateRealmUserAsync(
            accessToken,
            userPayload,
            conflictMessage,
            failureMessage,
            cancellationToken);

        await AssignRealmRoleAsync(accessToken, userId.ToString(), realmRole, cancellationToken);
        await KeycloakPasswordHelper.SetUserPasswordAsync(
            _httpClient,
            GetAdminRealmUrl(),
            accessToken,
            userId.ToString(),
            password,
            cancellationToken);

        return userId;
    }

    private async Task<Guid> CreateRealmUserAsync(
        string accessToken,
        CreateUserRequest userPayload,
        string conflictMessage,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetAdminRealmUrl()}/users")
        {
            Content = JsonContent.Create(userPayload)
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
        if (createResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new ConflictException(conflictMessage);

        if (!createResponse.IsSuccessStatusCode)
        {
            if (createResponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                    $"No fue posible crear la cuenta porque el realm '{_options.Realm}' no existe en Keycloak.",
                    createResponse,
                    cancellationToken));
            }

            if (createResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                    "No fue posible autenticar el cliente administrativo contra Keycloak.",
                    createResponse,
                    cancellationToken));
            }

            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                failureMessage,
                createResponse,
                cancellationToken));
        }

        var location = createResponse.Headers.Location?.ToString();
        var keycloakUserId = ExtractUserIdFromLocationHeader(location);
        if (!Guid.TryParse(keycloakUserId, out var userId))
        {
            throw new ExternalDependencyException(
                $"Keycloak devolvió un id de usuario no válido ('{keycloakUserId}').");
        }

        return userId;
    }

    private async Task<KeycloakUserRepresentation?> FindUserByEmailAsync(
        string accessToken,
        string email,
        CancellationToken cancellationToken)
    {
        var encodedEmail = Uri.EscapeDataString(email);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/users?email={encodedEmail}&exact=true&briefRepresentation=false");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                "No fue posible consultar el usuario en Keycloak.",
                response,
                cancellationToken));
        }

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken)
            ?? [];
        return users.FirstOrDefault();
    }

    private async Task<bool> UserHasRealmRoleAsync(
        string accessToken,
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/users/{userId}/role-mappings/realm");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        var roles = await response.Content.ReadFromJsonAsync<List<KeycloakRoleRepresentation>>(cancellationToken)
            ?? [];
        return roles.Any(role =>
            string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> UserHasPasswordCredentialAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/users/{userId}/credentials");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        var credentials = await response.Content.ReadFromJsonAsync<List<KeycloakCredentialRepresentation>>(cancellationToken)
            ?? [];
        return credentials.Any(credential =>
            string.Equals(credential.Type, "password", StringComparison.OrdinalIgnoreCase));
    }

    private async Task PersistOperatorSetupAttributesAsync(
        string accessToken,
        string userId,
        string firstName,
        string lastName,
        string email,
        string setupCodeHash,
        DateTimeOffset expiresUtc,
        CancellationToken cancellationToken)
    {
        var payload = new UpdateOperatorSetupAttributesRequest(
            Id: userId,
            Username: email,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            Enabled: false,
            EmailVerified: true,
            Attributes: new Dictionary<string, List<string>>
            {
                [OperatorSetupCodeHelper.SetupCodeHashAttribute] = [setupCodeHash],
                [OperatorSetupCodeHelper.SetupCodeExpiresAttribute] = [expiresUtc.ToString("O")]
            });

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GetAdminRealmUrl()}/users/{userId}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                "No fue posible guardar el código de activación del operador en Keycloak.",
                response,
                cancellationToken));
        }
    }

    private async Task<KeycloakUserRepresentation> GetUserByIdAsync(
        string accessToken,
        string userId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"No se encontró el usuario con Id={userId} en Keycloak.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                "No fue posible consultar el usuario en Keycloak.",
                response,
                cancellationToken));
        }

        return await response.Content.ReadFromJsonAsync<KeycloakUserRepresentation>(cancellationToken)
            ?? throw new ExternalDependencyException("Keycloak devolvió una representación de usuario vacía.");
    }

    private async Task UpdateUserActivationAsync(
        string accessToken,
        string userId,
        bool enabled,
        bool clearSetupAttributes,
        CancellationToken cancellationToken)
    {
        var user = await GetUserByIdAsync(accessToken, userId, cancellationToken);
        var username = !string.IsNullOrWhiteSpace(user.Username)
            ? user.Username
            : user.Email ?? userId;

        var attributes = user.Attributes is null
            ? new Dictionary<string, List<string>>()
            : new Dictionary<string, List<string>>(user.Attributes);

        if (clearSetupAttributes)
        {
            attributes[OperatorSetupCodeHelper.SetupCodeHashAttribute] = [];
            attributes[OperatorSetupCodeHelper.SetupCodeExpiresAttribute] = [];
        }

        var payload = new UpdateOperatorSetupAttributesRequest(
            Id: userId,
            Username: username,
            Email: user.Email ?? username,
            FirstName: user.FirstName ?? string.Empty,
            LastName: user.LastName ?? string.Empty,
            Enabled: enabled,
            EmailVerified: true,
            Attributes: attributes);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GetAdminRealmUrl()}/users/{userId}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                "No fue posible activar la cuenta del operador en Keycloak.",
                response,
                cancellationToken));
        }
    }

    private static string? GetAttributeValue(KeycloakUserRepresentation user, string attributeName)
    {
        if (user.Attributes is null
            || !user.Attributes.TryGetValue(attributeName, out var values)
            || values.Count == 0)
        {
            return null;
        }

        return values[0];
    }

    public async Task<IReadOnlyList<OperatorIdentityDto>> GetOperatorsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var roleName = Uri.EscapeDataString(_options.OperatorRole);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GetAdminRealmUrl()}/roles/{roleName}/users?first=0&max=1000");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                    "No fue posible consultar operadores en Keycloak.",
                    response,
                    cancellationToken));
            }

            var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken)
                ?? [];

            return users
                .Where(user => Guid.TryParse(user.Id, out _))
                .Select(user => new OperatorIdentityDto(
                    OperatorId: Guid.Parse(user.Id),
                    FirstName: user.FirstName ?? string.Empty,
                    LastName: user.LastName ?? string.Empty,
                    Email: user.Email ?? string.Empty,
                    IsActive: user.Enabled))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalDependencyException(
                "No fue posible comunicarse con Keycloak durante la consulta de operadores.",
                ex);
        }
    }

    public async Task DeactivateOperatorAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            await UpdateUserActivationAsync(
                accessToken,
                operatorId.ToString(),
                enabled: false,
                clearSetupAttributes: false,
                cancellationToken);
        }
        catch (NotFoundException)
        {
            throw new NotFoundException($"No existe un operador con Id={operatorId}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalDependencyException(
                $"No fue posible comunicarse con Keycloak al desactivar el operador con Id={operatorId}.",
                ex);
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
        {
            formValues["client_secret"] = _options.AdminClientSecret;
        }

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetBaseUrl()}/realms/{_options.AdminRealm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            if (tokenResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
            {
                throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                    "No fue posible obtener token administrativo de Keycloak. Verifica AdminClientId/AdminUsername/AdminPassword.",
                    tokenResponse,
                    cancellationToken));
            }

            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                "No fue posible obtener token administrativo de Keycloak.",
                tokenResponse,
                cancellationToken));
        }

        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokenBody is null || string.IsNullOrWhiteSpace(tokenBody.AccessToken))
        {
            throw new ExternalDependencyException("Keycloak no devolvió un token administrativo válido.");
        }

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
            if (getRoleResponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                    $"No fue posible recuperar el rol '{roleName}' en Keycloak. Verifica que exista en el realm '{_options.Realm}'.",
                    getRoleResponse,
                    cancellationToken));
            }

            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                $"No fue posible recuperar el rol '{roleName}' en Keycloak.",
                getRoleResponse,
                cancellationToken));
        }

        var roleRepresentation = await getRoleResponse.Content.ReadFromJsonAsync<KeycloakRoleRepresentation>(cancellationToken);
        if (roleRepresentation is null || string.IsNullOrWhiteSpace(roleRepresentation.Id))
        {
            throw new ExternalDependencyException(
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
            throw new ExternalDependencyException(await BuildKeycloakErrorAsync(
                $"No fue posible asignar el rol '{roleName}' al operador creado.",
                assignRoleResponse,
                cancellationToken));
        }
    }

    private static string ExtractUserIdFromLocationHeader(string? locationHeader)
    {
        if (string.IsNullOrWhiteSpace(locationHeader))
        {
            throw new ExternalDependencyException("Keycloak no devolvió la cabecera Location al crear el operador.");
        }

        return locationHeader.TrimEnd('/').Split('/').Last();
    }

    private static async Task<string> BuildKeycloakErrorAsync(
        string contextMessage,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return $"{contextMessage} StatusCode={(int)response.StatusCode}.";
        }

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
        bool EmailVerified,
        Dictionary<string, List<string>>? Attributes = null);

    private sealed record UpdateOperatorSetupAttributesRequest(
        string Id,
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        bool EmailVerified,
        Dictionary<string, List<string>> Attributes);

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

        [JsonPropertyName("username")]
        public string? Username { get; init; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; init; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, List<string>>? Attributes { get; init; }
    }

    private sealed class KeycloakCredentialRepresentation
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }
    }
}
