using MediatR;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Commands.AssignOperatorToMission;

public sealed class AssignOperatorToMissionHandler : IRequestHandler<AssignOperatorToMissionCommand>
{
    private readonly IMissionRepository _repository;
    private readonly IOperatorValidationService _operatorValidationService;

    public AssignOperatorToMissionHandler(
        IMissionRepository repository,
        IOperatorValidationService operatorValidationService)
    {
        _repository = repository;
        _operatorValidationService = operatorValidationService;
    }

    public async Task Handle(AssignOperatorToMissionCommand request, CancellationToken cancellationToken)
    {
        var isActiveOperator = await _operatorValidationService
            .IsActiveOperatorAsync(request.OperatorId, cancellationToken);

        if (!isActiveOperator)
            throw new NotFoundException($"No se encontró un operador activo con Id={request.OperatorId}.");

        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.AssignOperator(request.OperatorId);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}
