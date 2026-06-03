using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;

public sealed class CancelLiveSessionHandler : IRequestHandler<CancelLiveSessionCommand>
{
    private readonly ILiveSessionRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public CancelLiveSessionHandler(
        ILiveSessionRepository repository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task Handle(CancelLiveSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdForOperatorAsync(request.SessionId, request.OperatorId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión {request.SessionId} para el operador {request.OperatorId}.");

        var assignedMissions = await _missionIntegrationService.GetAssignedMissionsForOperatorAsync(
            request.OperatorId,
            cancellationToken);

        if (!assignedMissions.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión de la sesión no está asignada al operador (RN-16).");

        try
        {
            session.Cancel();
        }
        catch (SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _repository.SaveAsync(session, cancellationToken);
        await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, _teamRepository, cancellationToken);
    }
}
