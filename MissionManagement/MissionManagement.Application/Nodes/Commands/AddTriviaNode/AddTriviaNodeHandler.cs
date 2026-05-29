using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.AddTriviaNode;

public sealed class AddTriviaNodeHandler : IRequestHandler<AddTriviaNodeCommand, Guid>
{
    private readonly IMissionRepository _repository;

    public AddTriviaNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddTriviaNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var nodeId = mission.AddTriviaNode(
            parentNodeId: request.ParentNodeId,
            questions: request.Questions,
            executionOrder: request.ExecutionOrder);

        await _repository.SaveAsync(mission, cancellationToken);
        return nodeId;
    }
}

