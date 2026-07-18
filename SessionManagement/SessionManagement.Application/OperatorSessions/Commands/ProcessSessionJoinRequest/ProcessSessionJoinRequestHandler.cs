using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Commands.ProcessSessionJoinRequest;

public sealed class ProcessSessionJoinRequestHandler : IRequestHandler<ProcessSessionJoinRequestCommand>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegration;
    private readonly IDomainEventPublisher _eventPublisher;

    public ProcessSessionJoinRequestHandler(
        ILiveSessionRepository sessionRepository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegration,
        IDomainEventPublisher eventPublisher)
    {
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _missionIntegration = missionIntegration;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(ProcessSessionJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdForOperatorAsync(
            request.SessionId, request.OperatorId, cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la sesión {request.SessionId} para el operador {request.OperatorId}.");

        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(
            request.OperatorId, cancellationToken);
        if (!assigned.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión de la sesión no está asignada al operador.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el equipo {request.TeamId}.");

        try
        {
            if (request.Approve)
            {
                team.AssignToSession(session.Id);
                session.ApproveJoinRequest(request.TeamId, request.OperatorId, team.Name);
                await _teamRepository.SaveAsync(team, cancellationToken);
            }
            else
            {
                session.RejectJoinRequest(request.TeamId, request.OperatorId);
            }

            await _sessionRepository.SaveAsync(session, cancellationToken);

            var events = session.DomainEvents.ToList();
            if (events.Count > 0)
            {
                await _eventPublisher.PublishAsync(events, cancellationToken);
                session.ClearDomainEvents();
            }
        }
        catch (SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }
    }
}
