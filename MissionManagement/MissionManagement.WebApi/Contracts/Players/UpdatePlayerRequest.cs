namespace MissionManagement.WebApi.Contracts.Players;

public sealed record UpdatePlayerRequest(
    string FirstName,
    string LastName,
    string Email
);
