using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.WebApi.Contracts.Nodes;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
[Authorize(Roles = "admin,operator")]
public sealed class NodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddRootNode(
        [FromRoute] MissionNodesRoute route,
        [FromBody] AddRootNodeRequest body,
        CancellationToken cancellationToken)
    {
        var nodeId = await _mediator.Send(body.ToCommand(route), cancellationToken);
        return CreatedAtAction(nameof(GetNodes), new { missionId = route.MissionId }, new { id = nodeId });
    }

    [HttpGet]
    public async Task<IActionResult> GetNodes(MissionNodesRoute route, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{stageId:guid}/games")]
    public async Task<IActionResult> GetGamesByStage(
        [FromRoute] MissionStageRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{nodeId:guid}")]
    public async Task<IActionResult> UpdateNode(
        [FromRoute] MissionNodeRoute route,
        [FromBody] UpdateNodeRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{nodeId:guid}")]
    public async Task<IActionResult> DeleteNode(MissionNodeRoute route, CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToCommand(), cancellationToken);
        return NoContent();
    }
}
