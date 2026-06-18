using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Contracts.TreasureHunts;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
[Authorize(Roles = "admin,operator")]
public sealed class TreasureHuntsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TreasureHuntsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{parentNodeId:guid}/treasure-hunts")]
    public async Task<IActionResult> AddTreasureHunt(
        [FromRoute] MissionParentNodeRoute route,
        [FromBody] AddTreasureHuntNodeRequest body,
        CancellationToken cancellationToken)
    {
        var treasureHuntNodeId = await _mediator.Send(body.ToCommand(route), cancellationToken);
        return CreatedAtAction(
            nameof(GetTreasureHuntById),
            new { missionId = route.MissionId, nodeId = treasureHuntNodeId },
            new { id = treasureHuntNodeId });
    }

    [HttpGet("{nodeId:guid}/treasure-hunts")]
    public async Task<IActionResult> GetTreasureHuntById(
        [FromRoute] MissionNodeRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToTreasureHuntByIdQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{nodeId:guid}/treasure-hunts")]
    public async Task<IActionResult> UpdateTreasureHunt(
        [FromRoute] MissionNodeRoute route,
        [FromBody] UpdateTreasureHuntNodeRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }
}
