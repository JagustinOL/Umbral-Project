using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Admins.Commands.CreateAdmin;
using MissionManagement.WebApi.Contracts.Admins;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/admins")]
public sealed class AdminsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var adminId = await _mediator.Send(new CreateAdminCommand(
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            Password: request.Password), cancellationToken);

        return Created(string.Empty, new { id = adminId });
    }
}
