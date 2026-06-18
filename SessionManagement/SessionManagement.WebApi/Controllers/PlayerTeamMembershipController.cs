using MediatR;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Mapping;

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
        [FromRoute] PlayerRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }
}
