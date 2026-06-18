using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Mapping;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/session-validation")]
[AllowAnonymous]
public sealed class MissionSessionValidationController : ControllerBase
{
    private readonly IMediator _mediator;

    public MissionSessionValidationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("has-open")]
    public async Task<IActionResult> HasOpenSessions(
        [FromRoute] MissionRoute route,
        CancellationToken cancellationToken)
    {
        var hasOpenSessions = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(new { hasOpenSessions });
    }
}
