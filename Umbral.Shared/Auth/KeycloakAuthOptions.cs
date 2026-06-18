using System.ComponentModel.DataAnnotations;

namespace Umbral.Shared.Auth;

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

    public string Authority => $"{BaseUrl.TrimEnd('/')}/realms/{Realm}";
}
