using SessionManagement.Application.Teams.Queries.GetPlayerTeamMembership;
using SessionManagement.WebApi.Contracts.Routes;

namespace SessionManagement.WebApi.Mapping;

public static class PlayerTeamMembershipMappings
{
    public static GetPlayerTeamMembershipQuery ToQuery(this PlayerRoute route) =>
        new(route.PlayerId);
}
