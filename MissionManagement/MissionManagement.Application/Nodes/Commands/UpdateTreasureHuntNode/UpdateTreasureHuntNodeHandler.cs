using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.UpdateTreasureHuntNode;

public sealed class UpdateTreasureHuntNodeHandler : IRequestHandler<UpdateTreasureHuntNodeCommand>
{
    private readonly IMissionRepository _repository;

    public UpdateTreasureHuntNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateTreasureHuntNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.UpdateTreasureHuntNode(
            nodeId: request.NodeId,
            instructions: request.Instructions,
            secretCode: request.SecretCode,
            destination: request.Destination,
            baseScore: request.BaseScore);

        await _repository.SaveAsync(mission, cancellationToken);
    }
}

