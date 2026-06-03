using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Commands.SubmitJoinRequest;

public sealed class SubmitJoinRequestHandler
    : IRequestHandler<SubmitJoinRequestCommand, SubmitJoinRequestResult>
{
    private readonly ITeamRepository _teamRepository;

    public SubmitJoinRequestHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<SubmitJoinRequestResult> Handle(
        SubmitJoinRequestCommand request,
        CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByCodeAsync(request.TeamCode, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró un equipo con código '{request.TeamCode}'.");

        await PlayerSingleTeamGuard.EnsureCanJoinOrCreateTeamAsync(
            _teamRepository,
            request.PlayerId,
            targetTeamId: team.Id,
            cancellationToken);

        team.SubmitJoinRequest(request.PlayerId, request.DisplayName);
        await _teamRepository.SaveAsync(team, cancellationToken);

        var createdRequest = team.JoinRequests
            .OrderByDescending(x => x.RequestedAtUtc)
            .First(x => x.PlayerRef == request.PlayerId);

        return new SubmitJoinRequestResult(createdRequest.Id, team.Id);
    }
}
