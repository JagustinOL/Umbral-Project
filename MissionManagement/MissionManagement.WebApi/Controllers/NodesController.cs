using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Nodes.Commands.AddRootNode;
using MissionManagement.Application.Nodes.Commands.DeleteNode;
using MissionManagement.Application.Nodes.Commands.UpdateNode;
using MissionManagement.Application.Nodes.Queries.GetGamesByStage;
using MissionManagement.Application.Nodes.Queries.GetNodesByMission;
using MissionManagement.WebApi.Contracts.Nodes;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
public sealed class NodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public NodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddRootNode([FromRoute] Guid missionId, [FromBody] AddRootNodeRequest request, CancellationToken cancellationToken)
    {
        var nodeId = await _mediator.Send(new AddRootNodeCommand(
            MissionId: missionId,
            Title: request.Title,
            Description: request.Description,
            ExecutionOrder: request.ExecutionOrder), cancellationToken);

        return CreatedAtAction(nameof(GetNodes), new { missionId }, new { id = nodeId });
    }

    [HttpGet]
    public async Task<IActionResult> GetNodes([FromRoute] Guid missionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNodesByMissionQuery(missionId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{stageId:guid}/games")]
    public async Task<IActionResult> GetGamesByStage(
        [FromRoute] Guid missionId,
        [FromRoute] Guid stageId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGamesByStageQuery(missionId, stageId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{nodeId:guid}")]
    public async Task<IActionResult> UpdateNode([FromRoute] Guid missionId, [FromRoute] Guid nodeId, [FromBody] UpdateNodeRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateNodeCommand(
            MissionId: missionId,
            NodeId: nodeId,
            Title: request.Title,
            Description: request.Description), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{nodeId:guid}")]
    public async Task<IActionResult> DeleteNode([FromRoute] Guid missionId, [FromRoute] Guid nodeId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteNodeCommand(missionId, nodeId), cancellationToken);
        return NoContent();
    }
}

