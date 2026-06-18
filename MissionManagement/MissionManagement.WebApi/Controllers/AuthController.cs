using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.WebApi.Contracts.Auth;
using MissionManagement.WebApi.Mapping;

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
    [AllowAnonymous]
    public async Task<IActionResult> Token(
        [FromBody] AuthenticateRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCommand(), cancellationToken);
        return Ok(ToTokenResponse(result));
    }

    [HttpPost("operator/setup-password")]
    [AllowAnonymous]
    public async Task<IActionResult> SetupOperatorPassword(
        [FromBody] SetupOperatorPasswordRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(body.ToCommand(), cancellationToken);
        return Ok(ToTokenResponse(result));
    }

    private static object ToTokenResponse(AuthTokenResult result) => new
    {
        accessToken = result.AccessToken,
        refreshToken = result.RefreshToken,
        expiresIn = result.ExpiresIn,
        userId = result.UserId,
        roles = result.Roles
    };
}
