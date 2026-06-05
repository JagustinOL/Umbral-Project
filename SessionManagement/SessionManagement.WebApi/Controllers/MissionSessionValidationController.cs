using MediatR;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.Missions.Queries.MissionHasOpenSessions;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/session-validation")]
public sealed class MissionSessionValidationController : ControllerBase
{
    private readonly IMediator _mediator;

    public MissionSessionValidationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("has-open")]
    public async Task<IActionResult> HasOpenSessions(
        [FromRoute] Guid missionId,
        CancellationToken cancellationToken)
    {
        var hasOpenSessions = await _mediator.Send(new MissionHasOpenSessionsQuery(missionId), cancellationToken);
        return Ok(new { hasOpenSessions });
    }
}
