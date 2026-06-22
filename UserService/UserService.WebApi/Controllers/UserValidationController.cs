using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Operators.Queries.IsActiveOperator;

namespace UserService.WebApi.Controllers;

[ApiController]
[Route("api/v1/operators/{operatorId:guid}/validation")]
[AllowAnonymous]
public sealed class UserValidationController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserValidationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("is-active")]
    public async Task<IActionResult> IsActiveOperator(
        [FromRoute] Guid operatorId,
        CancellationToken cancellationToken)
    {
        var isActive = await _mediator.Send(new IsActiveOperatorQuery(operatorId), cancellationToken);
        return Ok(new { isActive });
    }
}
