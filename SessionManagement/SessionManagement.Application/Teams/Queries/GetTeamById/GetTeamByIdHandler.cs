using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Queries.GetTeamById;

public sealed class GetTeamByIdHandler : IRequestHandler<GetTeamByIdQuery, TeamDetailsDto>
{
    private readonly ITeamRepository _teamRepository;

    public GetTeamByIdHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<TeamDetailsDto> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        return new TeamDetailsDto(
            TeamId: team.Id,
            Name: team.Name,
            TeamCode: team.Code.Value,
            IsLocked: team.IsLocked,
            CurrentSessionRef: team.CurrentSessionRef,
            IsDisbanded: team.IsDisbanded,
            Members: team.Members
                .OrderBy(x => x.JoinedAtUtc)
                .Select(x => new TeamMemberDto(
                    PlayerRef: x.PlayerRef,
                    DisplayName: x.DisplayName,
                    Role: x.Role.ToString(),
                    JoinedAtUtc: x.JoinedAtUtc))
                .ToList());
    }
}
