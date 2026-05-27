using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.UpdateNode;

public sealed class UpdateNodeHandler : IRequestHandler<UpdateNodeCommand>
{
    private readonly IMissionRepository _repository;

    public UpdateNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.UpdateNode(request.NodeId, request.Title, request.Description);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

