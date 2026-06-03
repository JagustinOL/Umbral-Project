using MediatR;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Missions.Commands.DeactivateMission;

public sealed class DeactivateMissionHandler : IRequestHandler<DeactivateMissionCommand>
{
    private readonly IMissionRepository _repository;
    private readonly ISessionValidationService _sessionValidationService;

    public DeactivateMissionHandler(
        IMissionRepository repository,
        ISessionValidationService sessionValidationService)
    {
        _repository = repository;
        _sessionValidationService = sessionValidationService;
    }

    public async Task Handle(DeactivateMissionCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.Id}.");

        var hasOpenSessions = await _sessionValidationService.HasOpenSessionsForMissionAsync(
            request.Id,
            cancellationToken);

        if (hasOpenSessions)
        {
            throw new ConflictException(
                $"No se puede desactivar la misión con Id={request.Id} porque tiene sesiones abiertas (RN-01).");
        }

        mission.Deactivate();
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

