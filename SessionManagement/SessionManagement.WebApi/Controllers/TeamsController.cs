using MediatR;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.Teams.Commands.CreateTeam;
using SessionManagement.Application.Teams.Commands.DisbandTeam;
using SessionManagement.Application.Teams.Commands.ProcessJoinRequest;
using SessionManagement.Application.Teams.Commands.RemoveMember;
using SessionManagement.Application.Teams.Commands.SubmitJoinRequest;
using SessionManagement.Application.Teams.Commands.UpdateTeam;
using SessionManagement.Application.Teams.Queries.GetPendingRequests;
using SessionManagement.Application.Teams.Queries.GetTeamById;
using SessionManagement.WebApi.Contracts.Teams;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var teamId = await _mediator.Send(new CreateTeamCommand(
            Name: request.Name,
            CreatorId: request.CreatorId,
            CreatorDisplayName: request.CreatorDisplayName), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { teamId }, new { id = teamId });
    }

    [HttpGet("{teamId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid teamId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTeamByIdQuery(teamId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid teamId,
        [FromBody] UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateTeamCommand(
            TeamId: teamId,
            NewName: request.NewName,
            RequestorId: request.RequestorId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> Disband(
        [FromRoute] Guid teamId,
        [FromQuery] Guid requestorId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisbandTeamCommand(
            TeamId: teamId,
            RequestorId: requestorId), cancellationToken);
        return NoContent();
    }

    [HttpPost("join-requests")]
    public async Task<IActionResult> SubmitJoinRequest(
        [FromBody] SubmitJoinRequestRequest request,
        CancellationToken cancellationToken)
    {
        var requestId = await _mediator.Send(new SubmitJoinRequestCommand(
            TeamCode: request.TeamCode,
            PlayerId: request.PlayerRef,
            DisplayName: request.DisplayName), cancellationToken);

        return Ok(new { requestId });
    }

    [HttpGet("{teamId:guid}/requests")]
    public async Task<IActionResult> GetPendingRequests(
        [FromRoute] Guid teamId,
        [FromQuery] Guid requestorId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPendingRequestsQuery(
            TeamId: teamId,
            RequestorId: requestorId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{teamId:guid}/requests/{requestId:guid}")]
    public async Task<IActionResult> ProcessJoinRequest(
        [FromRoute] Guid teamId,
        [FromRoute] Guid requestId,
        [FromBody] ProcessJoinRequestRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ProcessJoinRequestCommand(
            TeamId: teamId,
            RequestId: requestId,
            IsApproved: request.Approve,
            RequestorId: request.RequestorId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}/members/{playerId:guid}")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] Guid teamId,
        [FromRoute] Guid playerId,
        [FromQuery] Guid requestorId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveMemberCommand(
            TeamId: teamId,
            PlayerId: playerId,
            RequestorId: requestorId), cancellationToken);
        return NoContent();
    }
}
