using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints.Commands.DeleteHint;

public sealed class DeleteHintHandler : IRequestHandler<DeleteHintCommand>
{
    private readonly IMissionRepository _repository;

    public DeleteHintHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteHintCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.DeleteHint(request.NodeId, request.HintId);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

