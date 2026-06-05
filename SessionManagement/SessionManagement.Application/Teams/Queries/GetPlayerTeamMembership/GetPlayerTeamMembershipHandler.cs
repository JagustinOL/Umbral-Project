using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Queries.GetPlayerTeamMembership;

public sealed class GetPlayerTeamMembershipHandler
    : IRequestHandler<GetPlayerTeamMembershipQuery, PlayerTeamMembershipDto>
{
    private readonly ITeamRepository _teamRepository;

    public GetPlayerTeamMembershipHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<PlayerTeamMembershipDto> Handle(
        GetPlayerTeamMembershipQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PlayerId == Guid.Empty)
            throw new ArgumentException("PlayerId no puede ser vacío.", nameof(request.PlayerId));

        var memberTeam = await _teamRepository.GetActiveTeamByPlayerRefAsync(
            request.PlayerId,
            cancellationToken);

        if (memberTeam is not null)
        {
            var member = memberTeam.Members.Single(m => m.PlayerRef == request.PlayerId);
            return new PlayerTeamMembershipDto(
                IsMember: true,
                TeamId: memberTeam.Id,
                TeamName: memberTeam.Name,
                TeamCode: memberTeam.Code.Value,
                Role: member.Role.ToString(),
                HasPendingJoinRequest: false,
                PendingTeamId: null);
        }

        var pendingTeam = await _teamRepository.GetActiveTeamWithPendingJoinRequestAsync(
            request.PlayerId,
            cancellationToken);

        if (pendingTeam is not null)
        {
            return new PlayerTeamMembershipDto(
                IsMember: false,
                TeamId: null,
                TeamName: pendingTeam.Name,
                TeamCode: pendingTeam.Code.Value,
                Role: null,
                HasPendingJoinRequest: true,
                PendingTeamId: pendingTeam.Id);
        }

        return new PlayerTeamMembershipDto(
            IsMember: false,
            TeamId: null,
            TeamName: null,
            TeamCode: null,
            Role: null,
            HasPendingJoinRequest: false,
            PendingTeamId: null);
    }
}
