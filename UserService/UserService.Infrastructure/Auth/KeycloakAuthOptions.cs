using System.ComponentModel.DataAnnotations;

namespace UserService.Infrastructure.Auth;

public sealed class KeycloakAuthOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    public string BaseUrl { get; init; } = "http://localhost:8081";

    [Required]
    public string Realm { get; init; } = "umbral-realm";

    [Required]
    public string WebClientId { get; init; } = "umbral-web";

    public string AdminRole { get; init; } = "admin";
    public string OperatorRole { get; init; } = "operator";
    public string PlayerRole { get; init; } = "player";

    /// <summary>
    /// Browser-facing Keycloak URL (e.g. http://localhost:8081 in Docker dev).
    /// When set, JWT validation accepts tokens issued for this host as well as <see cref="BaseUrl"/>.
    /// </summary>
    public string? PublicBaseUrl { get; init; }

    public string Authority => BuildAuthority(BaseUrl);

    public string? PublicAuthority =>
        string.IsNullOrWhiteSpace(PublicBaseUrl) ? null : BuildAuthority(PublicBaseUrl);

    private string BuildAuthority(string baseUrl) =>
        $"{baseUrl.TrimEnd('/')}/realms/{Realm}";
}
