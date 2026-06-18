namespace MissionManagement.WebApi.Contracts.Auth;

public sealed record SetupOperatorPasswordRequest(
    string Email,
    string SetupCode,
    string Password
);
