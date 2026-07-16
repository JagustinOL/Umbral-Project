using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Auth.Queries.ValidateToken;
using UserService.Application.Common.Interfaces;
using UserService.WebApi.Contracts.Auth;
using UserService.WebApi.Mapping;

namespace UserService.WebApi.Controllers;

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

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest body,
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

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate(CancellationToken cancellationToken)
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();

        var token = authHeader["Bearer ".Length..].Trim();
        var result = await _mediator.Send(new ValidateTokenQuery(token), cancellationToken);

        return Ok(new
        {
            userId = result.UserId,
            email = result.Email,
            firstName = result.FirstName,
            lastName = result.LastName,
            role = result.Role,
            status = result.Status,
            roles = result.Roles
        });
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
