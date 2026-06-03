using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
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

        session.JoinTeam(request.TeamId, request.JoinCode);
        await _repository.SaveAsync(session, cancellationToken);

        await TeamSessionLockService.AssignTeamToSessionAsync(
            request.TeamId,
            session.Id,
            _teamRepository,
            cancellationToken);

        return session.Id;
    }
}

