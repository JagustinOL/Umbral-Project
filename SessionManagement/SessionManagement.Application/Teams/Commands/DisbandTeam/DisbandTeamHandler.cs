using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.DisbandTeam;

public sealed class DisbandTeamHandler : IRequestHandler<DisbandTeamCommand>
{
    private readonly ITeamRepository _teamRepository;

    public DisbandTeamHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(DisbandTeamCommand request, CancellationToken cancellationToken)
    {
        TeamRequestorValidation.EnsureRequestorId(request.RequestorId);

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        team.Disband(request.RequestorId);
        await _teamRepository.SaveAsync(team, cancellationToken);
    }
}
