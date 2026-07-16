using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.WebApi.Contracts.OperatorSessions;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Mapping;
using SessionManagement.WebApi.Authorization;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/operators/{operatorId:guid}")]
[Authorize(Roles = "admin,operator")]
[EnsureOperatorMatchesRoute]
public sealed class OperatorSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperatorSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("missions")]
    public async Task<IActionResult> GetAssignedMissions(
        [FromRoute] OperatorRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToAssignedMissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sessions/open")]
    public async Task<IActionResult> GetOpenSessions(
        [FromRoute] OperatorRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToOpenSessionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateLiveSession(
        [FromRoute] OperatorRoute route,
        [FromBody] CreateLiveSessionRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCreateLiveSessionCommand(route), cancellationToken);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:guid}/teams")]
    public async Task<IActionResult> GetSessionTeams(
        [FromRoute] OperatorSessionRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToSessionTeamsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("sessions/{sessionId:guid}/start")]
    public async Task<IActionResult> StartLiveSession(
        [FromRoute] OperatorSessionRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToStartLiveSessionCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPut("sessions/{sessionId:guid}/finalize")]
    public async Task<IActionResult> FinalizeLiveSession(
        [FromRoute] OperatorSessionRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToFinalizeLiveSessionCommand(), cancellationToken);
        return NoContent();
    }

    [HttpPut("sessions/{sessionId:guid}/cancel")]
    public async Task<IActionResult> CancelLiveSession(
        [FromRoute] OperatorSessionRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToCancelLiveSessionCommand(), cancellationToken);
        return NoContent();
    }
}
