using MediatR;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Repositories;
using System.Net.Http;

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

        bool hasOpenSessions;
        try
        {
            hasOpenSessions = await _sessionValidationService.HasOpenSessionsForMissionAsync(
                request.Id,
                cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            throw new ConflictException(
                $"No fue posible validar sesiones abiertas para la misión con Id={request.Id}. " +
                "La desactivación fue bloqueada para proteger RN-01. " +
                $"Detalle técnico: {ex.Message}");
        }

        if (hasOpenSessions)
        {
            throw new ConflictException(
                $"No se puede desactivar la misión con Id={request.Id} porque tiene sesiones abiertas (RN-01).");
        }

        mission.Deactivate();
        await _repository.SaveAsync(mission, cancellationToken);
    }
}

