using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Missions.Commands.ActivateMission;
using MissionManagement.Application.Missions.Commands.CreateMission;
using MissionManagement.Application.Missions.Commands.DeactivateMission;
using MissionManagement.Application.Missions.Commands.UpdateMissionDetails;
using MissionManagement.Application.Missions.Queries.GetMissionById;
using MissionManagement.Application.Missions.Queries.GetMissionNodeValidations;
using MissionManagement.Application.Missions.Queries.GetMissions;
using MissionManagement.WebApi.Contracts.Missions;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions")]
public sealed class MissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMissionRequest request, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateMissionCommand(
            Title: request.Title,
            Description: request.Description,
            Difficulty: request.Difficulty,
            MaxDurationMinutes: request.MaxDurationMinutes), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMissionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/node-validations")]
    public async Task<IActionResult> GetNodeValidations([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMissionNodeValidationsQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDetails([FromRoute] Guid id, [FromBody] UpdateMissionDetailsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateMissionDetailsCommand(
            Id: id,
            Title: request.Title,
            Description: request.Description,
            MaxDurationMinutes: request.MaxDurationMinutes), cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<IActionResult> Activate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateMissionCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateMissionCommand(id), cancellationToken);
        return NoContent();
    }
}

