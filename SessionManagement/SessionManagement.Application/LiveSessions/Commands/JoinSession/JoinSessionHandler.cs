using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.LiveSessions.Commands.JoinSession;

public sealed class JoinSessionHandler : IRequestHandler<JoinSessionCommand, Guid>
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

    public async Task<Guid> Handle(JoinSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByJoinCodeAsync(request.JoinCode, cancellationToken);
        if (session is null)
            throw new NotFoundException($"No se encontró una sesión con código '{request.JoinCode}'.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        try
        {
            team.AssignToSession(session.Id);
        }
        catch (SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        try
        {
            session.JoinTeam(request.TeamId, request.JoinCode);
        }
        catch (SessionDomainException ex) when (team.CurrentSessionRef == session.Id)
        {
            return session.Id;
        }

        await _repository.SaveAsync(session, cancellationToken);
        await _teamRepository.SaveAsync(team, cancellationToken);

        var events = session.DomainEvents.ToList();
        if (events.Count > 0)
        {
            await _eventPublisher.PublishAsync(events, cancellationToken);
            session.ClearDomainEvents();
        }

        return session.Id;
    }
}
