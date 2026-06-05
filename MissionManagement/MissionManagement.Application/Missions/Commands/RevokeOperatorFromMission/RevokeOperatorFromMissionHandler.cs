using MediatR;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Commands.RevokeOperatorFromMission;

public sealed class RevokeOperatorFromMissionHandler : IRequestHandler<RevokeOperatorFromMissionCommand>
{
    private readonly IMissionRepository _repository;
    private readonly ISessionValidationService _sessionValidationService;

    public RevokeOperatorFromMissionHandler(
        IMissionRepository repository,
        ISessionValidationService sessionValidationService)
    {
        _repository = repository;
        _sessionValidationService = sessionValidationService;
    }

    public async Task Handle(RevokeOperatorFromMissionCommand request, CancellationToken cancellationToken)
    {
        var isSupervisingMission = await _sessionValidationService
            .IsSupervisingMissionAsync(request.OperatorId, request.MissionId, cancellationToken);

        if (isSupervisingMission)
            throw new ConflictException(
                $"No se puede revocar el operador con Id={request.OperatorId} porque supervisa una sesión activa de la misión con Id={request.MissionId}.");

        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        mission.RevokeOperator(request.OperatorId);
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

