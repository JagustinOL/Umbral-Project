using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Nodes.Commands.AddTreasureHuntNode;
using MissionManagement.Application.Nodes.Commands.UpdateTreasureHuntNode;
using MissionManagement.Application.Nodes.Queries.GetTreasureHuntNodeById;
using MissionManagement.Domain.ValueObjects;
using MissionManagement.WebApi.Contracts.TreasureHunts;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
public sealed class TreasureHuntsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TreasureHuntsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{parentNodeId:guid}/treasure-hunts")]
    public async Task<IActionResult> AddTreasureHunt(
        [FromRoute] Guid missionId,
        [FromRoute] Guid parentNodeId,
        [FromBody] AddTreasureHuntNodeRequest request,
        CancellationToken cancellationToken)
    {
        var treasureHuntNodeId = await _mediator.Send(new AddTreasureHuntNodeCommand(
            MissionId: missionId,
            ParentNodeId: parentNodeId,
            Instructions: request.Instructions,
            SecretCode: request.SecretCode,
            Destination: new GpsCoordinate(request.Destination.Latitude, request.Destination.Longitude),
            ExecutionOrder: request.ExecutionOrder), cancellationToken);

        return CreatedAtAction(nameof(GetTreasureHuntById), new { missionId, nodeId = treasureHuntNodeId }, new { id = treasureHuntNodeId });
    }

    [HttpGet("{nodeId:guid}/treasure-hunts")]
    public async Task<IActionResult> GetTreasureHuntById(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTreasureHuntNodeByIdQuery(missionId, nodeId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{nodeId:guid}/treasure-hunts")]
    public async Task<IActionResult> UpdateTreasureHunt(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        [FromBody] UpdateTreasureHuntNodeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateTreasureHuntNodeCommand(
            MissionId: missionId,
            NodeId: nodeId,
            Instructions: request.Instructions,
            SecretCode: request.SecretCode,
            Destination: new GpsCoordinate(request.Destination.Latitude, request.Destination.Longitude)), cancellationToken);

        return NoContent();
    }
}

