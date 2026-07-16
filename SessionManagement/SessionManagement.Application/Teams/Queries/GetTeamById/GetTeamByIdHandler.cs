using MediatR;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Queries.GetTeamById;

public sealed class GetTeamByIdHandler : IRequestHandler<GetTeamByIdQuery, TeamDetailsDto>
{
    private readonly ITeamRepository _teamRepository;
    private readonly ILiveSessionRepository _liveSessionRepository;

    public GetTeamByIdHandler(
        ITeamRepository teamRepository,
        ILiveSessionRepository liveSessionRepository)
    {
        _teamRepository = teamRepository;
        _liveSessionRepository = liveSessionRepository;
    }

    public async Task<TeamDetailsDto> Handle(GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        // Si ya está asignado a la sesión, la solicitud pendiente dejó de aplicar.
        Guid? pendingSessionJoinRef = null;
        if (team.CurrentSessionRef is null)
        {
            pendingSessionJoinRef = await _liveSessionRepository
                .FindOpenSessionIdWithPendingJoinByTeamAsync(team.Id, cancellationToken);
        }

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
                .ToList(),
            PendingSessionJoinRef: pendingSessionJoinRef);
    }
}
