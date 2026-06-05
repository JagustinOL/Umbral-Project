namespace MissionManagement.WebApi.Contracts.Auth;

public sealed class AuthenticateRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
