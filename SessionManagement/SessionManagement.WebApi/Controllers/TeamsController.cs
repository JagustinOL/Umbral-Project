using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.Teams.Commands.DisbandTeam;
using SessionManagement.Application.Teams.Commands.RemoveMember;
using SessionManagement.Application.Teams.Queries.GetPendingRequests;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Contracts.Teams;
using SessionManagement.WebApi.Mapping;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/teams")]
[Authorize(Roles = "player")]
public sealed class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTeamRequest body,
        CancellationToken cancellationToken)
    {
        var teamId = await _mediator.Send(body.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { teamId }, new { id = teamId });
    }

    [HttpGet("{teamId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] TeamRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] TeamRoute route,
        [FromBody] UpdateTeamRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Disband(
        [FromRoute(Name = "teamId")] Guid teamId,
        [FromQuery] Guid requestorId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisbandTeamCommand(teamId, requestorId), cancellationToken);
        return NoContent();
    }

    [HttpPost("join-requests")]
    public async Task<IActionResult> SubmitJoinRequest(
        [FromBody] SubmitJoinRequestRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCommand(), cancellationToken);
        return Ok(new { requestId = result.RequestId, teamId = result.TeamId });
    }

    [HttpGet("{teamId:guid}/requests")]
    public async Task<IActionResult> GetPendingRequests(
        [FromRoute(Name = "teamId")] Guid teamId,
        [FromQuery] Guid requestorId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPendingRequestsQuery(teamId, requestorId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPut("{teamId:guid}/requests/{requestId:guid}")]
    public async Task<IActionResult> ProcessJoinRequest(
        [FromRoute] TeamJoinRequestRoute route,
        [FromBody] ProcessJoinRequestRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}/members/{playerId:guid}")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute(Name = "teamId")] Guid teamId,
        [FromRoute(Name = "playerId")] Guid playerId,
        [FromQuery] Guid requestorId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new RemoveMemberCommand(teamId, playerId, requestorId),
            cancellationToken);
        return NoContent();
    }
}
