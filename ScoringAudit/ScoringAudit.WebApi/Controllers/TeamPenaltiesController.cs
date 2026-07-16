using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScoringAudit.WebApi.Contracts.Routes;
using ScoringAudit.WebApi.Mapping;

namespace ScoringAudit.WebApi.Controllers;

[ApiController]
[Route("api/v1/sessions/{sessionId:guid}/teams/{teamId:guid}/penalties")]
[Authorize(Roles = "admin,operator,player")]
public sealed class TeamPenaltiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeamPenaltiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeamPenalties(
        [FromRoute] SessionTeamRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToPenaltiesQuery(), cancellationToken);
        return Ok(result);
    }
}
