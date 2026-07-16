using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;

namespace UserService.Infrastructure.External.Keycloak;

public sealed class KeycloakAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakAuthService(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<AuthTokenResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var formValues = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.WebClientId,
            ["username"] = username,
            ["password"] = password
        };

        if (!string.IsNullOrWhiteSpace(_options.WebClientSecret))
            formValues["client_secret"] = _options.WebClientSecret;

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetBaseUrl()}/realms/{_options.Realm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalDependencyException(
                "No fue posible comunicarse con Keycloak durante la autenticación.",
                ex);
        }

        using (tokenResponse)
        {
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var keycloakError = await TryReadKeycloakErrorAsync(tokenResponse, cancellationToken);

                if (keycloakError?.Error is "invalid_client")
                {
                    throw new ExternalDependencyException(
                        $"El cliente OIDC '{_options.WebClientId}' no está configurado en Keycloak. Reinicia mission-management-service para aplicar el bootstrap automático.");
                }

                if (tokenResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest
                    && keycloakError?.Error is "invalid_grant" or "invalid_user_credentials")
                {
                    throw new UnauthorizedException("Credenciales inválidas o cuenta deshabilitada.");
                }

                if (tokenResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                {
                    throw new UnauthorizedException("Credenciales inválidas o cuenta deshabilitada.");
                }

                throw new ExternalDependencyException(
                    await BuildKeycloakErrorAsync(
                        "No fue posible autenticar al usuario en Keycloak.",
                        tokenResponse,
                        cancellationToken));
            }

            var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new ExternalDependencyException("Keycloak no devolvió un cuerpo de token válido.");

            if (string.IsNullOrWhiteSpace(tokenBody.AccessToken))
                throw new ExternalDependencyException("Keycloak no devolvió un access token válido.");

            var payload = KeycloakJwtReader.ReadPayload(tokenBody.AccessToken);
            var roles = payload.RealmAccess?.Roles?
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            return new AuthTokenResult(
                AccessToken: tokenBody.AccessToken,
                RefreshToken: tokenBody.RefreshToken,
                ExpiresIn: tokenBody.ExpiresIn,
                UserId: Guid.Parse(payload.Sub),
                Roles: roles);
        }
    }

    public async Task<AuthTokenResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedException("Refresh token inválido o expirado.");

        var formValues = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.WebClientId,
            ["refresh_token"] = refreshToken
        };

        if (!string.IsNullOrWhiteSpace(_options.WebClientSecret))
            formValues["client_secret"] = _options.WebClientSecret;

        using var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{GetBaseUrl()}/realms/{_options.Realm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        HttpResponseMessage tokenResponse;
        try
        {
            tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalDependencyException(
                "No fue posible comunicarse con Keycloak durante la renovación del token.",
                ex);
        }

        using (tokenResponse)
        {
            if (!tokenResponse.IsSuccessStatusCode)
            {
                if (tokenResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                    throw new UnauthorizedException("Refresh token inválido o expirado.");

                throw new ExternalDependencyException(
                    await BuildKeycloakErrorAsync(
                        "No fue posible renovar el token en Keycloak.",
                        tokenResponse,
                        cancellationToken));
            }

            var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new ExternalDependencyException("Keycloak no devolvió un cuerpo de token válido.");

            if (string.IsNullOrWhiteSpace(tokenBody.AccessToken))
                throw new ExternalDependencyException("Keycloak no devolvió un access token válido.");

            var payload = KeycloakJwtReader.ReadPayload(tokenBody.AccessToken);
            var roles = payload.RealmAccess?.Roles?
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [];

            return new AuthTokenResult(
                AccessToken: tokenBody.AccessToken,
                RefreshToken: tokenBody.RefreshToken ?? refreshToken,
                ExpiresIn: tokenBody.ExpiresIn,
                UserId: Guid.Parse(payload.Sub),
                Roles: roles);
        }
    }

    private string GetBaseUrl() => _options.BaseUrl.TrimEnd('/');

    private static async Task<string> BuildKeycloakErrorAsync(
        string message,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return $"{message} StatusCode={(int)response.StatusCode}. Body={body}";
    }

    private static async Task<KeycloakOAuthError?> TryReadKeycloakErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<KeycloakOAuthError>(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private sealed class KeycloakOAuthError
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
