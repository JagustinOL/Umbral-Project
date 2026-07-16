using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.OperatorSessions.Commands.ProcessSessionJoinRequest;
using SessionManagement.Application.OperatorSessions.Queries.GetSessionJoinRequests;
using SessionManagement.WebApi.Auth;
using SessionManagement.WebApi.Contracts.OperatorSessions;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/sessions/{sessionId:guid}")]
[Authorize(Roles = "admin,operator")]
public sealed class SessionJoinRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public SessionJoinRequestsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("join-requests")]
    public async Task<IActionResult> GetJoinRequests(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        var result = await _mediator.Send(
            new GetSessionJoinRequestsQuery(operatorId, sessionId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("join-requests/{teamId:guid}/decision")]
    public async Task<IActionResult> DecideJoinRequest(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        [FromBody] ProcessJoinRequestDecisionRequest body,
        CancellationToken cancellationToken)
    {
        var operatorId = RequireOperatorId();
        var approve = string.Equals(body.Decision, "Approve", StringComparison.OrdinalIgnoreCase);
        if (!approve && !string.Equals(body.Decision, "Reject", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Decision debe ser 'Approve' o 'Reject'." });

        await _mediator.Send(
            new ProcessSessionJoinRequestCommand(operatorId, sessionId, teamId, approve),
            cancellationToken);
        return NoContent();
    }

    private Guid RequireOperatorId()
    {
        if (_currentUser.UserId is not Guid operatorId || operatorId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuario autenticado no válido.");
        return operatorId;
    }
}
