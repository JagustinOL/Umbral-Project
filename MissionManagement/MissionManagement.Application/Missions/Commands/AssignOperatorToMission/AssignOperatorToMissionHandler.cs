using MediatR;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Commands.AssignOperatorToMission;

public sealed class AssignOperatorToMissionHandler : IRequestHandler<AssignOperatorToMissionCommand>
{
    private readonly IMissionRepository _repository;

    public AssignOperatorToMissionHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AssignOperatorToMissionCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.AssignOperator(request.OperatorId);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

