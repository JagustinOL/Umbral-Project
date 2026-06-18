using System.ComponentModel.DataAnnotations;

namespace MissionManagement.Infrastructure.External.Keycloak;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    [Required]
    public string BaseUrl { get; init; } = "http://localhost:8081";

    [Required]
    public string Realm { get; init; } = "umbral-realm";

    [Required]
    public string AdminRealm { get; init; } = "master";

    [Required]
    public string AdminClientId { get; init; } = "admin-cli";

    public string? AdminClientSecret { get; init; }

    [Required]
    public string AdminUsername { get; init; } = "admin";

    [Required]
    public string AdminPassword { get; init; } = "admin";

    [Required]
    public string OperatorRole { get; init; } = "operator";

    [Required]
    public string PlayerRole { get; init; } = "player";

    [Required]
    public string AdminRole { get; init; } = "admin";

    [Required]
    public string WebClientId { get; init; } = "umbral-web";

    public string? WebClientSecret { get; init; }

    public string? DefaultAdminEmail { get; init; }

    public string? DefaultAdminPassword { get; init; }

    public string DefaultAdminFirstName { get; init; } = "UMBRAL";

    public string DefaultAdminLastName { get; init; } = "Administrator";

    public string OperatorSetupCodeSalt { get; init; } = "umbral-operator-setup-salt";

    public int OperatorSetupCodeTtlDays { get; init; } = 7;
}
