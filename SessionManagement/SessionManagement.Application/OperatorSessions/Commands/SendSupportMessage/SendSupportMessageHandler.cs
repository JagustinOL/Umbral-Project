using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Commands.SendSupportMessage;

public sealed class SendSupportMessageHandler : IRequestHandler<SendSupportMessageCommand>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IMissionIntegrationService _missionIntegration;
    private readonly IDomainEventPublisher _eventPublisher;

    public SendSupportMessageHandler(
        ILiveSessionRepository sessionRepository,
        IMissionIntegrationService missionIntegration,
        IDomainEventPublisher eventPublisher)
    {
        _sessionRepository = sessionRepository;
        _missionIntegration = missionIntegration;
        _eventPublisher = eventPublisher;
    }

    public async Task Handle(SendSupportMessageCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdForOperatorAsync(
            request.SessionId, request.OperatorId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró la sesión {request.SessionId}.");

        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(
            request.OperatorId, cancellationToken);
        if (!assigned.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión no está asignada al operador.");

        try
        {
            session.SendSupportMessage(request.TeamId, request.OperatorId, request.Message);
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
