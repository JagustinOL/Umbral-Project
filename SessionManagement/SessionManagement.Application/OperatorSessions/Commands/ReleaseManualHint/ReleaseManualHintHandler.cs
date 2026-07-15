using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Commands.ReleaseManualHint;

public sealed class ReleaseManualHintHandler : IRequestHandler<ReleaseManualHintCommand>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IMissionIntegrationService _missionIntegration;
    private readonly IDomainEventPublisher _eventPublisher;

    public ReleaseManualHintHandler(
        ILiveSessionRepository sessionRepository,
        IMissionIntegrationService missionIntegration,
        IDomainEventPublisher eventPublisher)
    {
        _sessionRepository = sessionRepository;
        _missionIntegration = missionIntegration;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(ReleaseManualHintCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdForOperatorAsync(
            request.SessionId, request.OperatorId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró la sesión {request.SessionId}.");

        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(
            request.OperatorId, cancellationToken);
        if (!assigned.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión no está asignada al operador (RN-16).");

        var rules = await NodeValidationRulesFactory.BuildAsync(
            _missionIntegration, session.MissionRef, cancellationToken);

        var currentNodeId = session.GetCurrentNodeForTeam(request.TeamId, rules);
        if (currentNodeId is null)
            throw new ConflictException(
                "El equipo ya completó todos los juegos; no se pueden liberar más pistas (RN-04).");

        var hints = await _missionIntegration.GetHintsForNodeAsync(
            session.MissionRef, currentNodeId.Value, cancellationToken);
        var hintData = hints.FirstOrDefault(h => h.Id == request.HintId)
            ?? throw new NotFoundException(
                $"No se encontró la pista {request.HintId} en el juego actual del equipo.");

        try
        {
            session.ReleaseHint(
                request.TeamId,
                request.HintId,
                currentNodeId.Value,
                hintData.PenaltyPoints,
                rules,
                wasManualRelease: true);

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
