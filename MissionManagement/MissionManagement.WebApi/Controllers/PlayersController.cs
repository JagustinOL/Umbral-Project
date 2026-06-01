using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Players.Commands.CreatePlayer;
using MissionManagement.Application.Players.Commands.DeactivatePlayer;
using MissionManagement.Application.Players.Commands.UpdatePlayer;
using MissionManagement.Application.Players.Queries.GetPlayerById;
using MissionManagement.Application.Players.Queries.GetPlayers;
using MissionManagement.WebApi.Contracts.Players;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayersController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlayerRequest request, CancellationToken cancellationToken)
    {
        var playerId = await _mediator.Send(new CreatePlayerCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { playerId }, new { id = playerId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlayersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{playerId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid playerId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlayerByIdQuery(playerId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{playerId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid playerId,
        [FromBody] UpdatePlayerRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdatePlayerCommand(
            PlayerId: playerId,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email), cancellationToken);

        return NoContent();
    }

    [HttpPut("{playerId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid playerId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivatePlayerCommand(playerId), cancellationToken);
        return NoContent();
    }
}
