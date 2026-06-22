namespace ScoringAudit.WebApi.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Username { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
