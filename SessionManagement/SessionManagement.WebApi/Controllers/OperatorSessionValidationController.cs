using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Mapping;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/operators/{operatorId:guid}")]
[AllowAnonymous]
public sealed class OperatorSessionValidationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperatorSessionValidationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("session-validation/has-active")]
    public async Task<IActionResult> HasActiveSessions(
        [FromRoute] OperatorRoute route,
        CancellationToken cancellationToken)
    {
        var hasActiveSessions = await _mediator.Send(route.ToHasActiveSessionsQuery(), cancellationToken);
        return Ok(new { hasActiveSessions });
    }

    [HttpGet("missions/{missionId:guid}/session-validation/is-supervising")]
    public async Task<IActionResult> IsSupervisingMission(
        [FromRoute] OperatorMissionRoute route,
        CancellationToken cancellationToken)
    {
        var isSupervising = await _mediator.Send(route.ToIsSupervisingMissionQuery(), cancellationToken);
        return Ok(new { isSupervising });
    }
}
