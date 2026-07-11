using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Nodes.Queries.GetNodePlayerContent;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
public sealed class PlayerNodeContentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayerNodeContentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{nodeId:guid}/player-content")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlayerContent(
        [FromRoute] MissionNodeRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetNodePlayerContentQuery(route.MissionId, route.NodeId),
            cancellationToken);
        return Ok(result);
    }
}
