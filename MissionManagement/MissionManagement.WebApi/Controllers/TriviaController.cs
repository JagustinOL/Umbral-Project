using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Contracts.Trivia;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
[Authorize(Roles = "admin,operator")]
public sealed class TriviaController : ControllerBase
{
    private readonly IMediator _mediator;

    public TriviaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{parentNodeId:guid}/trivia")]
    public async Task<IActionResult> AddTrivia(
        [FromRoute] MissionParentNodeRoute route,
        [FromBody] AddTriviaNodeRequest body,
        CancellationToken cancellationToken)
    {
        var triviaNodeId = await _mediator.Send(body.ToCommand(route), cancellationToken);
        return CreatedAtAction(
            nameof(GetTriviaById),
            new { missionId = route.MissionId, nodeId = triviaNodeId },
            new { id = triviaNodeId });
    }

    [HttpGet("{nodeId:guid}/trivia")]
    public async Task<IActionResult> GetTriviaById(MissionNodeRoute route, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToTriviaByIdQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{nodeId:guid}/trivia")]
    public async Task<IActionResult> UpdateTrivia(
        [FromRoute] MissionNodeRoute route,
        [FromBody] UpdateTriviaNodeRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }
}
