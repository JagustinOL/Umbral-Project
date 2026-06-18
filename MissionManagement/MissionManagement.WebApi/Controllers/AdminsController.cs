using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.WebApi.Contracts.Admins;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/admins")]
[Authorize(Roles = "admin")]
public sealed class AdminsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAdminRequest body,
        CancellationToken cancellationToken)
    {
        var adminId = await _mediator.Send(body.ToCommand(), cancellationToken);
        return Created(string.Empty, new { id = adminId });
    }
}
