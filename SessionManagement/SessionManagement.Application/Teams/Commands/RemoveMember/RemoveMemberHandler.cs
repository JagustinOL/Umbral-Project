using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.RemoveMember;

public sealed class RemoveMemberHandler : IRequestHandler<RemoveMemberCommand>
{
    private readonly ITeamRepository _teamRepository;

    public RemoveMemberHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        TeamRequestorValidation.EnsureRequestorId(request.RequestorId);

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        team.RemoveMember(request.PlayerId, request.RequestorId);
        await _teamRepository.SaveAsync(team, cancellationToken);
    }
}
