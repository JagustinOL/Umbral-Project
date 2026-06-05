using MediatR;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.LiveSessions.Commands.JoinSession;

public sealed class JoinSessionHandler : IRequestHandler<JoinSessionCommand, Guid>
{
    private readonly ILiveSessionRepository _repository;
    private readonly ITeamRepository _teamRepository;

    public JoinSessionHandler(ILiveSessionRepository repository, ITeamRepository teamRepository)
    {
        _repository = repository;
        _teamRepository = teamRepository;
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
            // Idempotente: el equipo ya estaba registrado en esta misma sesión.
            return session.Id;
        }

        await _repository.SaveAsync(session, cancellationToken);
        await _teamRepository.SaveAsync(team, cancellationToken);

        return session.Id;
    }
}

