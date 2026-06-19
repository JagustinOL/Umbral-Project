using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.LiveSessions.Queries.GetActiveSessions;
using SessionManagement.WebApi.Contracts.LiveSessions;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Mapping;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/live-sessions")]
[Authorize(Roles = "player")]
public sealed class LiveSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LiveSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveSessionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinSession(
        [FromBody] JoinSessionRequest body,
        CancellationToken cancellationToken)
    {
        var sessionId = await _mediator.Send(body.ToCommand(), cancellationToken);
        return Ok(new { sessionId });
    }

    [HttpGet("{sessionId:guid}/teams/{teamId:guid}/current-stage")]
    public async Task<IActionResult> GetTeamCurrentStage(
        [FromRoute] LiveSessionTeamRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/teams/{teamId:guid}/treasure-hunt-code")]
    public async Task<IActionResult> SubmitTreasureHuntCode(
        [FromRoute] LiveSessionTeamRoute route,
        [FromBody] SubmitTreasureHuntCodeRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCommand(route), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/teams/{teamId:guid}/trivia-answer")]
    public async Task<IActionResult> SubmitTriviaAnswer(
        [FromRoute] LiveSessionTeamRoute route,
        [FromBody] SubmitTriviaAnswerRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCommand(route), cancellationToken);
        return Ok(result);
    }
}
