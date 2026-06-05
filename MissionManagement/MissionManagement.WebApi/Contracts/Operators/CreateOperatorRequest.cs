namespace MissionManagement.WebApi.Contracts.Operators;

public sealed record CreateOperatorRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password
);
