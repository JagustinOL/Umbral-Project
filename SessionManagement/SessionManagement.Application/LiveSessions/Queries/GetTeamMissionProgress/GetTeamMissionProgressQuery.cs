using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Queries.GetTeamMissionProgress;

public sealed record GetTeamMissionProgressQuery(
    Guid SessionId,
    Guid TeamId
) : IRequest<TeamMissionProgressDto>;
