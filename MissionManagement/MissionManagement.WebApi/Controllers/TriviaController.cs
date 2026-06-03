using MediatR;
using Microsoft.AspNetCore.Mvc;
using MissionManagement.Application.Nodes.Commands.AddTriviaNode;
using MissionManagement.Application.Nodes.Commands.UpdateTriviaNode;
using MissionManagement.Application.Nodes.Queries.GetTriviaNodeById;
using MissionManagement.Domain.ValueObjects;
using MissionManagement.WebApi.Contracts.Trivia;

namespace MissionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/missions/{missionId:guid}/nodes")]
public sealed class TriviaController : ControllerBase
{
    private readonly IMediator _mediator;

    public TriviaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{parentNodeId:guid}/trivia")]
    public async Task<IActionResult> AddTrivia(
        [FromRoute] Guid missionId,
        [FromRoute] Guid parentNodeId,
        [FromBody] AddTriviaNodeRequest request,
        CancellationToken cancellationToken)
    {
        var triviaNodeId = await _mediator.Send(new AddTriviaNodeCommand(
            MissionId: missionId,
            ParentNodeId: parentNodeId,
            Questions: request.Questions
                .Select(q => new TriviaQuestion(q.Prompt, q.Options, q.CorrectOptionIndex))
                .ToList(),
            ExecutionOrder: request.ExecutionOrder), cancellationToken);

        return CreatedAtAction(nameof(GetTriviaById), new { missionId, nodeId = triviaNodeId }, new { id = triviaNodeId });
    }

    [HttpGet("{nodeId:guid}/trivia")]
    public async Task<IActionResult> GetTriviaById(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTriviaNodeByIdQuery(missionId, nodeId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{nodeId:guid}/trivia")]
    public async Task<IActionResult> UpdateTrivia(
        [FromRoute] Guid missionId,
        [FromRoute] Guid nodeId,
        [FromBody] UpdateTriviaNodeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateTriviaNodeCommand(
            MissionId: missionId,
            NodeId: nodeId,
            Questions: request.Questions
                .Select(q => new TriviaQuestion(q.Prompt, q.Options, q.CorrectOptionIndex))
                .ToList()), cancellationToken);

        return NoContent();
    }
}

