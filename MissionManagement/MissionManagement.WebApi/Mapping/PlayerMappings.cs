using MissionManagement.Application.Players.Commands.CreatePlayer;
using MissionManagement.Application.Players.Commands.DeactivatePlayer;
using MissionManagement.Application.Players.Commands.UpdatePlayer;
using MissionManagement.Application.Players.Queries.GetPlayerById;
using MissionManagement.WebApi.Contracts.Players;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Mapping;

public static class PlayerMappings
{
    public static CreatePlayerCommand ToCommand(this CreatePlayerRequest body) =>
        new(
            FirstName: body.FirstName,
            LastName: body.LastName,
            Email: body.Email,
            Password: body.Password);

    public static GetPlayerByIdQuery ToQuery(this PlayerRoute route) =>
        new(route.PlayerId);

    public static UpdatePlayerCommand ToCommand(this UpdatePlayerRequest body, PlayerRoute route) =>
        new(
            PlayerId: route.PlayerId,
            FirstName: body.FirstName,
            LastName: body.LastName,
            Email: body.Email);

    public static DeactivatePlayerCommand ToCommand(this PlayerRoute route) =>
        new(route.PlayerId);
}
