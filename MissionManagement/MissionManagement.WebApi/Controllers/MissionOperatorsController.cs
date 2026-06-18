using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.WebApi.Contracts.Operators;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/operators")]
[Authorize(Roles = "admin")]
public sealed class MissionOperatorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MissionOperatorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Assign(
        [FromRoute] MissionNodesRoute route,
        [FromBody] AssignOperatorToMissionRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{operatorId:guid}")]
    public async Task<IActionResult> Revoke(
        [FromRoute] MissionOperatorRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToCommand(), cancellationToken);
        return NoContent();
    }
}
