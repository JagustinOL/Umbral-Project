using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.LiveSessions.Commands.JoinSession;

public sealed class JoinSessionHandler : IRequestHandler<JoinSessionCommand, JoinSessionResultDto>
{
    private readonly ILiveSessionRepository _repository;
    private readonly ITeamRepository _teamRepository;
    private readonly IDomainEventPublisher _eventPublisher;

    public JoinSessionHandler(
        ILiveSessionRepository repository,
        ITeamRepository teamRepository,
        IDomainEventPublisher eventPublisher)
    {
        _repository = repository;
        _teamRepository = teamRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<JoinSessionResultDto> Handle(JoinSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByJoinCodeAsync(request.JoinCode, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró una sesión con código '{request.JoinCode}'.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        if (team.IsLocked)
            throw new ConflictException(
                "El equipo participa en una sesión en curso y no puede unirse a otra.");

        if (team.CurrentSessionRef is Guid otherSession && otherSession != session.Id)
            throw new ConflictException(
                "El equipo ya está registrado en otra sesión. Espere a que finalice antes de unirse a otra.");

        // Ya aprobado/registrado → idempotente
        if (session.RegisteredTeamIds.Contains(team.Id))
        {
            return new JoinSessionResultDto(session.Id, JoinRequestStatus.Approved.ToString(), null);
        }

        var existingPending = session.JoinRequests.FirstOrDefault(
            x => x.TeamId == team.Id && x.Status == JoinRequestStatus.Pending);
        if (existingPending is not null)
        {
            return new JoinSessionResultDto(
                session.Id,
                JoinRequestStatus.Pending.ToString(),
                existingPending.Id);
        }

        try
        {
            var joinRequest = session.SubmitJoinRequest(request.TeamId, request.JoinCode);
            await _repository.SaveAsync(session, cancellationToken);

            var events = session.DomainEvents.ToList();
            if (events.Count > 0)
            {
                await _eventPublisher.PublishAsync(events, cancellationToken);
                session.ClearDomainEvents();
            }

            return new JoinSessionResultDto(
                session.Id,
                joinRequest.Status.ToString(),
                joinRequest.Id);
        }
        catch (SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }
    }
}
