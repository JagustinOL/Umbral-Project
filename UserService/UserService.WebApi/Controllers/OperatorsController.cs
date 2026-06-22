using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Operators.Queries.GetOperators;
using UserService.WebApi.Contracts.Operators;
using UserService.WebApi.Contracts.Routes;
using UserService.WebApi.Mapping;

namespace UserService.WebApi.Controllers;

[ApiController]
[Route("api/v1/operators")]
[Authorize(Roles = "admin")]
public sealed class OperatorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperatorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOperatorRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, new { id = result.OperatorId, setupCode = result.SetupCode });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOperatorsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{operatorId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] OperatorRoute route,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToCommand(), cancellationToken);
        return NoContent();
    }
}
