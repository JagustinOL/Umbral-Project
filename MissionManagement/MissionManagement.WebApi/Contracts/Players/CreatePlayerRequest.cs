namespace MissionManagement.WebApi.Contracts.Players;

public sealed record CreatePlayerRequest(
    string FirstName,
    string LastName,
    string Email
);
