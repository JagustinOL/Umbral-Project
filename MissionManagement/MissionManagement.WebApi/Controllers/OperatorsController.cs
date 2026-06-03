using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Operators.Commands.CreateOperator;
using MissionManagement.Application.Operators.Commands.DeactivateOperator;
using MissionManagement.Application.Operators.Queries.GetOperators;
using MissionManagement.WebApi.Contracts.Operators;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/operators")]
public sealed class OperatorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperatorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatorRequest request, CancellationToken cancellationToken)
    {
        var operatorId = await _mediator.Send(new CreateOperatorCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            Password: request.Password), cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { }, new { id = operatorId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOperatorsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{operatorId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid operatorId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateOperatorCommand(operatorId), cancellationToken);
        return NoContent();
    }
}
