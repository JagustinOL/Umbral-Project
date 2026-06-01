using MediatR;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.LiveSessions.Commands.JoinSession;
using SessionManagement.Application.LiveSessions.Commands.SubmitTreasureHuntCode;
using SessionManagement.Application.LiveSessions.Commands.SubmitTriviaAnswer;
using SessionManagement.Application.LiveSessions.Queries.GetActiveSessions;
using SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentStage;
using SessionManagement.WebApi.Contracts.LiveSessions;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/live-sessions")]
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
    public async Task<IActionResult> JoinSession([FromBody] JoinSessionRequest request, CancellationToken cancellationToken)
    {
        var sessionId = await _mediator.Send(new JoinSessionCommand(
            JoinCode: request.JoinCode,
            TeamId: request.TeamId), cancellationToken);

        return Ok(new { sessionId });
    }

    [HttpGet("{sessionId:guid}/teams/{teamId:guid}/current-stage")]
    public async Task<IActionResult> GetTeamCurrentStage(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTeamCurrentStageQuery(
            SessionId: sessionId,
            TeamId: teamId), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/teams/{teamId:guid}/treasure-hunt-code")]
    public async Task<IActionResult> SubmitTreasureHuntCode(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        [FromBody] SubmitTreasureHuntCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitTreasureHuntCodeCommand(
            SessionId: sessionId,
            TeamId: teamId,
            NodeId: request.NodeId,
            FoundCode: request.FoundCode), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{sessionId:guid}/teams/{teamId:guid}/trivia-answer")]
    public async Task<IActionResult> SubmitTriviaAnswer(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        [FromBody] SubmitTriviaAnswerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SubmitTriviaAnswerCommand(
            SessionId: sessionId,
            TeamId: teamId,
            NodeId: request.NodeId,
            Answer: request.Answer), cancellationToken);

        return Ok(result);
    }
}

