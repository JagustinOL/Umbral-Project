using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.OperatorSessions.Commands.ApplyManualPenalty;
using SessionManagement.Application.OperatorSessions.Commands.ReconcileSessionScoring;
using SessionManagement.Application.OperatorSessions.Commands.ReleaseManualHint;
using SessionManagement.Application.OperatorSessions.Commands.SendSupportMessage;
using SessionManagement.Application.OperatorSessions.Commands.ToggleSessionPause;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorSessionBoard;
using SessionManagement.WebApi.Auth;
using SessionManagement.WebApi.Contracts.OperatorSessions;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/sessions/{sessionId:guid}")]
[Authorize(Roles = "admin,operator")]
public sealed class OperatorSessionControlController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OperatorSessionControlController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("operator-board")]
    public async Task<IActionResult> GetOperatorBoard(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        var board = await _mediator.Send(
            new GetOperatorSessionBoardQuery(operatorId, sessionId),
            cancellationToken);
        return Ok(board);
    }

    [HttpPost("teams/{teamId:guid}/hints/release")]
    public async Task<IActionResult> ReleaseHint(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        [FromBody] ReleaseManualHintRequest body,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        await _mediator.Send(
            new ReleaseManualHintCommand(operatorId, sessionId, teamId, body.HintId),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("teams/{teamId:guid}/penalties")]
    public async Task<IActionResult> ApplyPenalty(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        [FromBody] ApplyManualPenaltyRequest body,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        await _mediator.Send(
            new ApplyManualPenaltyCommand(operatorId, sessionId, teamId, body.Points, body.Reason),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("teams/{teamId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        [FromBody] SendSupportMessageRequest body,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        await _mediator.Send(
            new SendSupportMessageCommand(operatorId, sessionId, teamId, body.Message),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("pause")]
    public async Task<IActionResult> TogglePause(
        [FromRoute] Guid sessionId,
        [FromBody] ToggleSessionPauseRequest? body,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        var status = await _mediator.Send(
            new ToggleSessionPauseCommand(operatorId, sessionId, body?.Reason),
            cancellationToken);
        return Ok(new { status });
    }

    [HttpPost("scoring/reconcile")]
    public async Task<IActionResult> ReconcileScoring(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        var result = await _mediator.Send(
            new ReconcileSessionScoringCommand(operatorId, sessionId),
            cancellationToken);
        return Ok(result);
    }

    private Guid RequireOperatorId()
    {
        if (_currentUser.UserId is not Guid operatorId || operatorId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuario autenticado no válido.");
        return operatorId;
    }
}
