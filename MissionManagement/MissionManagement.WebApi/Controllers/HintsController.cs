using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Hints.Commands.AddHint;
using MissionManagement.Application.Hints.Commands.DeleteHint;
using MissionManagement.Application.Hints.Commands.UpdateHint;
using MissionManagement.Application.Hints.Queries.GetHintsByNode;
using MissionManagement.WebApi.Contracts.Hints;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes/{nodeId:guid}/hints")]
public sealed class HintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public HintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddHint(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        [FromForm] AddHintRequest request,
        CancellationToken cancellationToken)
    {
        var hintId = await _mediator.Send(new AddHintCommand(
            MissionId: missionId,
            NodeId: nodeId,
            Content: request.Content,
            Attachment: request.Attachment), cancellationToken);

        return CreatedAtAction(nameof(GetHintsByNode), new { missionId, nodeId }, new { id = hintId });
    }

    [HttpGet]
    public async Task<IActionResult> GetHintsByNode(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHintsByNodeQuery(missionId, nodeId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{hintId:guid}")]
    public async Task<IActionResult> UpdateHint(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        [FromRoute] Guid hintId,
        [FromBody] UpdateHintRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateHintCommand(
            MissionId: missionId,
            NodeId: nodeId,
            HintId: hintId,
            Content: request.Content), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{hintId:guid}")]
    public async Task<IActionResult> DeleteHint(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        [FromRoute] Guid hintId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteHintCommand(missionId, nodeId, hintId), cancellationToken);
        return NoContent();
    }
}

