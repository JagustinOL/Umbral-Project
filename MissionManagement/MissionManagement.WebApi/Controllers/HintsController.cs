using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.WebApi.Contracts.Hints;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes/{nodeId:guid}/hints")]
[Authorize(Roles = "admin,operator")]
public sealed class HintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public HintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddHint(
        [FromRoute] HintParentRoute route,
        [FromBody] AddHintRequest body,
        CancellationToken cancellationToken)
    {
        var hintId = await _mediator.Send(body.ToCommand(route), cancellationToken);
        return CreatedAtAction(
            nameof(GetHintsByNode),
            new { missionId = route.MissionId, nodeId = route.NodeId },
            new { id = hintId });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetHintsByNode(
        [FromRoute] HintParentRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{hintId:guid}")]
    public async Task<IActionResult> UpdateHint(
        [FromRoute] HintRoute route,
        [FromBody] UpdateHintRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{hintId:guid}")]
    public async Task<IActionResult> DeleteHint(
        [FromRoute] HintRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToCommand(), cancellationToken);
        return NoContent();
    }
}
