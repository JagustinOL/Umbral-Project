using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.DeleteNode;

public sealed class DeleteNodeHandler : IRequestHandler<DeleteNodeCommand>
{
    private readonly IMissionRepository _repository;

    public DeleteNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.DeleteNode(request.NodeId);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

