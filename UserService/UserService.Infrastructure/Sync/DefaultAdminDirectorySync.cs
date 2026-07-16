using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;
using UserService.Infrastructure.External.Keycloak;

namespace UserService.Infrastructure.Sync;

public sealed class DefaultAdminDirectorySync
{
    private readonly IUserRepository _userRepository;
    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;
    private readonly ILogger<DefaultAdminDirectorySync> _logger;

    public DefaultAdminDirectorySync(
        IUserRepository userRepository,
        IHttpClientFactory httpClientFactory,
        Microsoft.Extensions.Options.IOptions<KeycloakOptions> options,
        ILogger<DefaultAdminDirectorySync> logger)
    {
        _userRepository = userRepository;
        _httpClient = httpClientFactory.CreateClient(nameof(KeycloakWebClientInitializer));
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TrySyncAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.DefaultAdminEmail))
            return true;

        var email = _options.DefaultAdminEmail.Trim();
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            return true;

        var userId = await FindKeycloakUserIdByEmailAsync(email, cancellationToken);
        if (userId is null)
            return false;

        await _userRepository.SaveAsync(
            User.Create(
                userId.Value,
                email,
                _options.DefaultAdminFirstName,
                _options.DefaultAdminLastName,
                UserRole.Admin,
                UserStatus.Active),
            cancellationToken);

        _logger.LogInformation("Synced default admin '{Email}' to users directory.", email);
        return true;
    }

    private async Task<Guid?> FindKeycloakUserIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var adminToken = await GetAdminAccessTokenAsync(cancellationToken);
        var encodedEmail = Uri.EscapeDataString(email);

        using var searchRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{GetAdminRealmUrl()}/users?email={encodedEmail}&exact=true");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        using var searchResponse = await _httpClient.SendAsync(searchRequest, cancellationToken);
        searchResponse.EnsureSuccessStatusCode();

        var users = await searchResponse.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(cancellationToken)
            ?? [];

        if (users.Count == 0 || string.IsNullOrWhiteSpace(users[0].Id))
            return null;

        return Guid.Parse(users[0].Id!);
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
            $"{_options.BaseUrl.TrimEnd('/')}/realms/{_options.AdminRealm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        using var tokenResponse = await _httpClient.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenBody = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Keycloak no devolvió un token administrativo válido.");

        if (string.IsNullOrWhiteSpace(tokenBody.AccessToken))
            throw new InvalidOperationException("Keycloak no devolvió un token administrativo válido.");

        return tokenBody.AccessToken;
    }

    private string GetAdminRealmUrl()
        => $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}";

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class KeycloakUserRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }
    }
}
