using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScoringAudit.WebApi.Contracts.Routes;
using ScoringAudit.WebApi.Mapping;

namespace ScoringAudit.WebApi.Controllers;

[ApiController]
[Route("api/v1/sessions/{sessionId:guid}/ranking")]
[Authorize(Roles = "admin,operator")]
public sealed class RankingController : ControllerBase
{
    private readonly IMediator _mediator;

    public RankingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetRanking(
        [FromRoute] SessionRoute route,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }
}
