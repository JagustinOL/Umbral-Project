using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.UpdateTeam;

public sealed class UpdateTeamHandler : IRequestHandler<UpdateTeamCommand>
{
    private readonly ITeamRepository _teamRepository;

    public UpdateTeamHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(UpdateTeamCommand request, CancellationToken cancellationToken)
    {
        TeamRequestorValidation.EnsureRequestorId(request.RequestorId);

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        var duplicatedName = await _teamRepository.ExistsByNameAsync(
            request.NewName,
            excludingTeamId: team.Id,
            cancellationToken: cancellationToken);

        if (duplicatedName)
            throw new ConflictException($"Ya existe un equipo con nombre '{request.NewName}'.");

        team.UpdateName(request.NewName, request.RequestorId);
        await _teamRepository.SaveAsync(team, cancellationToken);
    }
}
