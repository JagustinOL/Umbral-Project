using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentNodeContent;

public sealed record GetTeamCurrentNodeContentQuery(
    Guid SessionId,
    Guid TeamId
) : IRequest<TeamCurrentNodeContentDto>;
