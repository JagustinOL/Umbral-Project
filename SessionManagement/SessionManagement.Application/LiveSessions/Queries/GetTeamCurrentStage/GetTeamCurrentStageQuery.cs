using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentStage;

public sealed record GetTeamCurrentStageQuery(
    Guid SessionId,
    Guid TeamId
) : IRequest<TeamCurrentStageDto>;

