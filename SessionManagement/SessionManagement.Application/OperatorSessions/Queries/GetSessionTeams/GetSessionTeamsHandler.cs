using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Queries.GetSessionTeams;

public sealed class GetSessionTeamsHandler : IRequestHandler<GetSessionTeamsQuery, SessionTeamsDto>
{
    private readonly ILiveSessionRepository _repository;
    private readonly IMissionIntegrationService _missionIntegrationService;

    public GetSessionTeamsHandler(
        ILiveSessionRepository repository,
        IMissionIntegrationService missionIntegrationService)
    {
        _repository = repository;
        _missionIntegrationService = missionIntegrationService;
    }

    public async Task<SessionTeamsDto> Handle(GetSessionTeamsQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdForOperatorAsync(request.SessionId, request.OperatorId, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró la sesión {request.SessionId} para el operador {request.OperatorId}.");

        var assignedMissions = await _missionIntegrationService.GetAssignedMissionsForOperatorAsync(
            request.OperatorId,
            cancellationToken);

        if (!assignedMissions.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión de la sesión no está asignada al operador (RN-16).");

        if (session.Status is LiveSessionStatus.Cancelled or LiveSessionStatus.Finalized)
            throw new ConflictException("La sesión ya está cerrada.");

        var teamIds = session.RegisteredTeamIds.ToList();
        return new SessionTeamsDto(
            SessionId: session.Id,
            TeamIds: teamIds,
            TeamCount: teamIds.Count);
    }
}

