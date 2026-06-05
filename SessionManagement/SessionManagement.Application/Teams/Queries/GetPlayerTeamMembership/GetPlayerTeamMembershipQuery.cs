using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.Teams.Queries.GetPlayerTeamMembership;

public sealed record GetPlayerTeamMembershipQuery(Guid PlayerId)
    : IRequest<PlayerTeamMembershipDto>;
