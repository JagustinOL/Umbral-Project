using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MissionManagement.WebApi.Auth;

public sealed class UserServiceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UserServiceAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHttpClientFactory httpClientFactory)
        : base(options, logger, encoder)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.Fail("Bearer token vacío.");

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(UserServiceAuthenticationHandler));
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/validate");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
                return AuthenticateResult.Fail("Token rechazado por UserService.");

            var body = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>(Context.RequestAborted);
            if (body is null || body.UserId == Guid.Empty)
                return AuthenticateResult.Fail("Respuesta de validación inválida.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, body.UserId.ToString()),
                new("sub", body.UserId.ToString()),
                new(ClaimTypes.Email, body.Email),
                new("email", body.Email),
                new("preferred_username", body.Email),
                new("given_name", body.FirstName),
                new("family_name", body.LastName),
            };

            foreach (var role in UserServiceValidatedUserClaims.BuildRoleClaims(body.Role, body.Roles))
                claims.Add(role);

            var identity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Fallo al validar token contra UserService.");
            return AuthenticateResult.Fail("No fue posible validar el token.");
        }
    }

    private sealed record ValidateTokenResponse(
        [property: JsonPropertyName("userId")] Guid UserId,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("firstName")] string FirstName,
        [property: JsonPropertyName("lastName")] string LastName,
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles);
}
