using MediatR;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.Teams.Queries.GetPlayerTeamMembership;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/players")]
public sealed class PlayerTeamMembershipController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayerTeamMembershipController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{playerId:guid}/team-membership")]
    public async Task<IActionResult> GetTeamMembership(
        [FromRoute] Guid playerId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPlayerTeamMembershipQuery(playerId),
            cancellationToken);

        return Ok(result);
    }
}
