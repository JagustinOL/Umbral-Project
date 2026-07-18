using MediatR;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Exceptions;
using SessionManagement.Domain.Repositories;

namespace SessionManagement.Application.OperatorSessions.Queries.GetSessionJoinRequests;

public sealed class GetSessionJoinRequestsHandler
    : IRequestHandler<GetSessionJoinRequestsQuery, IReadOnlyList<SessionJoinRequestDto>>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IMissionIntegrationService _missionIntegration;

    public GetSessionJoinRequestsHandler(
        ILiveSessionRepository sessionRepository,
        ITeamRepository teamRepository,
        IMissionIntegrationService missionIntegration)
    {
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _missionIntegration = missionIntegration;
    }

    public async Task<IReadOnlyList<SessionJoinRequestDto>> Handle(
        GetSessionJoinRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdForOperatorAsync(
            request.SessionId, request.OperatorId, cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la sesión {request.SessionId} para el operador {request.OperatorId}.");

        var assigned = await _missionIntegration.GetAssignedMissionsForOperatorAsync(
            request.OperatorId, cancellationToken);
        if (!assigned.Any(x => x.MissionId == session.MissionRef))
            throw new NotFoundException("La misión de la sesión no está asignada al operador.");

        var result = new List<SessionJoinRequestDto>();
        foreach (var joinRequest in session.JoinRequests.OrderByDescending(x => x.RequestedAtUtc))
        {
            var team = await _teamRepository.GetByIdAsync(joinRequest.TeamId, cancellationToken);
            result.Add(new SessionJoinRequestDto(
                RequestId: joinRequest.Id,
                TeamId: joinRequest.TeamId,
                TeamName: team?.Name,
                Status: joinRequest.Status.ToString(),
                RequestedAtUtc: joinRequest.RequestedAtUtc,
                ResolvedAtUtc: joinRequest.ResolvedAtUtc));
        }

        return result;
    }
}
