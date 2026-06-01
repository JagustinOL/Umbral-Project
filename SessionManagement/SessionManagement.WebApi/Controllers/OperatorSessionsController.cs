using MediatR;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;
using SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorAssignedMissions;
using SessionManagement.Application.OperatorSessions.Queries.GetSessionTeams;
using SessionManagement.WebApi.Contracts.OperatorSessions;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/operators/{operatorId:guid}")]
public sealed class OperatorSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperatorSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("missions")]
    public async Task<IActionResult> GetAssignedMissions(
        [FromRoute] Guid operatorId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOperatorAssignedMissionsQuery(operatorId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateLiveSession(
        [FromRoute] Guid operatorId,
        [FromBody] CreateLiveSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateLiveSessionCommand(
            OperatorId: operatorId,
            MissionId: request.MissionId), cancellationToken);

        return Ok(result);
    }

    [HttpGet("sessions/{sessionId:guid}/teams")]
    public async Task<IActionResult> GetSessionTeams(
        [FromRoute] Guid operatorId,
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSessionTeamsQuery(
            OperatorId: operatorId,
            SessionId: sessionId), cancellationToken);

        return Ok(result);
    }

    [HttpPut("sessions/{sessionId:guid}/start")]
    public async Task<IActionResult> StartLiveSession(
        [FromRoute] Guid operatorId,
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new StartLiveSessionCommand(
            OperatorId: operatorId,
            SessionId: sessionId), cancellationToken);

        return NoContent();
    }
}

