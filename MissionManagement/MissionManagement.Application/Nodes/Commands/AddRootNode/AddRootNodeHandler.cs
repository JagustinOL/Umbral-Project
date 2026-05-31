using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.AddRootNode;

public sealed class AddRootNodeHandler : IRequestHandler<AddRootNodeCommand, Guid>
{
    private readonly IMissionRepository _repository;

    public AddRootNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddRootNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        var node = MissionNode.Create(
            title: request.Title,
            description: request.Description,
            nodeType: MissionNodeType.Stage,
            executionOrder: request.ExecutionOrder,
            baseScore: 0,
            parentNodeId: null);

        mission.AddRootNode(node);
        await _repository.SaveAsync(mission, cancellationToken);

        return node.Id;
    }
}

