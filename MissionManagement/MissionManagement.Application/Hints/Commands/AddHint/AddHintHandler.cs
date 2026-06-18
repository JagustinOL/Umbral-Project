using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints.Commands.AddHint;

public sealed class AddHintHandler : IRequestHandler<AddHintCommand, Guid>
{
    private readonly IMissionRepository _repository;

    public AddHintHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddHintCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var node = mission.FindNodeById(request.NodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={request.NodeId}.");

        var nextOrder = node.Hints.Count + 1;
        var hint = Hint.Create(
            missionNodeId: request.NodeId,
            order: nextOrder,
            content: request.Content,
            penaltyPoints: 0);

        mission.AddHintToNode(request.NodeId, hint);
        await _repository.SaveAsync(mission, cancellationToken);

        return hint.Id;
    }
}

