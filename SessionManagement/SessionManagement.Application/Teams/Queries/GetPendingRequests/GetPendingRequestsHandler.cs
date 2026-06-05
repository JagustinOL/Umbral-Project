using MediatR;
using SessionManagement.Application.Common;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.Teams.Queries.GetPendingRequests;

public sealed class GetPendingRequestsHandler : IRequestHandler<GetPendingRequestsQuery, IReadOnlyList<JoinRequestDto>>
{
    private readonly ITeamRepository _teamRepository;

    public GetPendingRequestsHandler(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<IReadOnlyList<JoinRequestDto>> Handle(GetPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        TeamRequestorValidation.EnsureRequestorId(request.RequestorId);

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"No se encontró el equipo con Id={request.TeamId}.");

        if (!team.IsLeader(request.RequestorId))
            throw new ConflictException("Solo el líder del equipo puede consultar solicitudes pendientes.");

        return team.JoinRequests
            .Where(x => x.Status == JoinRequestStatus.Pending)
            .OrderByDescending(x => x.RequestedAtUtc)
            .Select(x => new JoinRequestDto(
                RequestId: x.Id,
                PlayerRef: x.PlayerRef,
                DisplayName: x.DisplayName,
                Status: x.Status.ToString(),
                RequestedAtUtc: x.RequestedAtUtc,
                ReviewedAtUtc: x.ReviewedAtUtc))
            .ToList();
    }
}
