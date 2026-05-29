using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints.Commands.UpdateHint;

public sealed class UpdateHintHandler : IRequestHandler<UpdateHintCommand>
{
    private readonly IMissionRepository _repository;

    public UpdateHintHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateHintCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.UpdateHint(
            nodeId: request.NodeId,
            hintId: request.HintId,
            newContent: request.Content);

        await _repository.SaveAsync(mission, cancellationToken);
    }
}

