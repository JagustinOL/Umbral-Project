using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Missions.Queries.GetMissions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.WebApi.Contracts.Missions;
using MissionManagement.WebApi.Contracts.Routes;
using MissionManagement.WebApi.Mapping;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions")]
[Authorize(Roles = "admin,operator")]
public sealed class MissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISessionValidationService _sessionValidationService;

    public MissionsController(
        IMediator mediator,
        ISessionValidationService sessionValidationService)
    {
        _mediator = mediator;
        _sessionValidationService = sessionValidationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMissionRequest body,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(body.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(MissionRoute route, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/node-validations")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNodeValidations(MissionRoute route, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(route.ToNodeValidationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/session-validation/has-open")]
    [AllowAnonymous]
    public async Task<IActionResult> HasOpenSessions(MissionRoute route, CancellationToken cancellationToken)
    {
        var hasOpenSessions = await _sessionValidationService.HasOpenSessionsForMissionAsync(
            route.Id, cancellationToken);
        return Ok(new { hasOpenSessions });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDetails(
        [FromRoute] MissionRoute route,
        [FromBody] UpdateMissionDetailsRequest body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(body.ToCommand(route), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate(MissionRoute route, CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToActivateCommand(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(MissionRoute route, CancellationToken cancellationToken)
    {
        await _mediator.Send(route.ToDeactivateCommand(), cancellationToken);
        return NoContent();
    }
}
