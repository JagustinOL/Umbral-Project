using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;

public sealed class FinalizeLiveSessionHandler : IRequestHandler<FinalizeLiveSessionCommand>
{
    private readonly ILiveSessionRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public FinalizeLiveSessionHandler(
        ILiveSessionRepository repository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task Handle(FinalizeLiveSessionCommand request, CancellationToken cancellationToken)
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
            session.Finalize();
        }
        catch (SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _repository.SaveAsync(session, cancellationToken);
        await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, _teamRepository, cancellationToken);
    }
}
