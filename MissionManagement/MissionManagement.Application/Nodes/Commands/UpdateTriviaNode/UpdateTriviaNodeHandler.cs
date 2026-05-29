using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Nodes.Commands.UpdateTriviaNode;

public sealed class UpdateTriviaNodeHandler : IRequestHandler<UpdateTriviaNodeCommand>
{
    private readonly IMissionRepository _repository;

    public UpdateTriviaNodeHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateTriviaNodeCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.UpdateTriviaNode(request.NodeId, request.Questions);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

