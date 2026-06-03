using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Missions.Commands.AssignOperatorToMission;
using MissionManagement.Application.Missions.Commands.RevokeOperatorFromMission;
using MissionManagement.WebApi.Contracts.Operators;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/operators")]
public sealed class MissionOperatorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MissionOperatorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Assign(
        [FromRoute] Guid missionId,
        [FromBody] AssignOperatorToMissionRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new AssignOperatorToMissionCommand(
            MissionId: missionId,
            OperatorId: request.OperatorId), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{operatorId:guid}")]
    public async Task<IActionResult> Revoke(
        [FromRoute] Guid missionId,
        [FromRoute] Guid operatorId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeOperatorFromMissionCommand(
            MissionId: missionId,
            OperatorId: operatorId), cancellationToken);

        return NoContent();
    }
}
