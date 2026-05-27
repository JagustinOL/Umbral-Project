using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Commands.DeactivateMission;

public sealed class DeactivateMissionHandler : IRequestHandler<DeactivateMissionCommand>
{
    private readonly IMissionRepository _repository;

    public DeactivateMissionHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeactivateMissionCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.Id}.");

        mission.Deactivate();
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

