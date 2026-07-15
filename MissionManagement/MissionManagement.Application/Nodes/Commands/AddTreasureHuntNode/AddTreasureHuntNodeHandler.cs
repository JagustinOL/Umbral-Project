using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.AddTreasureHuntNode;

public sealed class AddTreasureHuntNodeHandler : IRequestHandler<AddTreasureHuntNodeCommand, Guid>
{
    private readonly IMissionRepository _repository;

    public AddTreasureHuntNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddTreasureHuntNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var nodeId = mission.AddTreasureHuntNode(
            parentNodeId: request.ParentNodeId,
            instructions: request.Instructions,
            secretCode: request.SecretCode,
            destination: request.Destination,
            executionOrder: request.ExecutionOrder,
            baseScore: request.BaseScore);

        await _repository.SaveAsync(mission, cancellationToken);
        return nodeId;
    }
}

