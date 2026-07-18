using UserService.Application.Players.Commands.CreatePlayer;
using UserService.Application.Players.Commands.DeactivatePlayer;
using UserService.Application.Players.Commands.UpdatePlayer;
using UserService.Application.Players.Queries.GetPlayerById;
using UserService.WebApi.Contracts.Players;
using UserService.WebApi.Contracts.Routes;

namespace UserService.WebApi.Mapping;

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
