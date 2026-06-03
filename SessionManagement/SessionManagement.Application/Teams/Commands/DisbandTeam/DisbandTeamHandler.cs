using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.DisbandTeam;

public sealed class DisbandTeamHandler : IRequestHandler<DisbandTeamCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ILiveSessionRepository _liveSessionRepository;

    public DisbandTeamHandler(
        ITeamRepository teamRepository,
        ILiveSessionRepository liveSessionRepository)
    {
        _teamRepository = teamRepository;
        _liveSessionRepository = liveSessionRepository;
    }

    public async Task Handle(DisbandTeamCommand request, CancellationToken cancellationToken)
    {
        TeamRequestorValidation.EnsureRequestorId(request.RequestorId);

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        if (!team.IsLocked && team.CurrentSessionRef is Guid sessionId)
        {
            var session = await _liveSessionRepository.GetByIdAsync(sessionId, cancellationToken);
            if (session?.Status is LiveSessionStatus.Active or LiveSessionStatus.Paused)
            {
                throw new ConflictException(
                    "No se puede disolver el equipo durante una sesión activa o pausada (RN-13).");
            }
        }

        try
        {
            team.Disband(request.RequestorId);
        }
        catch (SessionDomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _teamRepository.SaveAsync(team, cancellationToken);
    }
}
