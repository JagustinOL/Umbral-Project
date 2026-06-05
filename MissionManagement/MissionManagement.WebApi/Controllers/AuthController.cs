using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Auth.Commands.AuthenticateUser;
using MissionManagement.WebApi.Contracts.Auth;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new AuthenticateUserCommand(request.Username, request.Password),
            cancellationToken);

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresIn = result.ExpiresIn,
            userId = result.UserId,
            roles = result.Roles
        });
    }
}
