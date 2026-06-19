using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Players.Queries.GetPlayers;
using MissionManagement.WebApi.Contracts.Players;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/players")]
[Authorize(Roles = "admin,operator,player")]
public sealed class PlayersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlayerRequest body,
        CancellationToken cancellationToken)
    {
        var playerId = await _mediator.Send(body.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { playerId }, new { id = playerId });
    }

    [HttpGet]
    [Authorize(Roles = "admin,operator")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlayersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{playerId:guid}")]
    [Authorize(Roles = "player")]
    public async Task<IActionResult> GetById(
        [FromRoute] PlayerRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{playerId:guid}")]
    [Authorize(Roles = "player")]
    public async Task<IActionResult> Update(
        [FromRoute] PlayerRoute route,
        [FromBody] UpdatePlayerRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpPut("{playerId:guid}/deactivate")]
    [Authorize(Roles = "player")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] PlayerRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToCommand(), cancellationToken);
        return NoContent();
    }
}
