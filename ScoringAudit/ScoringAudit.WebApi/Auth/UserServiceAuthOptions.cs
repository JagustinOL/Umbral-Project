using System.ComponentModel.DataAnnotations;

namespace ScoringAudit.WebApi.Auth;

public sealed class UserServiceAuthOptions
{
    public const string SectionName = "UserService";

    [Required]
    public string BaseUrl { get; init; } = "http://localhost:5284";
}
