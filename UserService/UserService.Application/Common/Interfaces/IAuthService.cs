namespace UserService.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthTokenResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record AuthTokenResult(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    Guid UserId,
    IReadOnlyList<string> Roles);
