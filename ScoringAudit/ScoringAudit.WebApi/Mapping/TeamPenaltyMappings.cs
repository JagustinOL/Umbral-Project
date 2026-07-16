using ScoringAudit.Application.Queries;
using ScoringAudit.WebApi.Contracts.Routes;

namespace ScoringAudit.WebApi.Mapping;

public static class TeamPenaltyMappings
{
    public static GetTeamPenaltiesQuery ToPenaltiesQuery(this SessionTeamRoute route) =>
        new(route.SessionId, route.TeamId);
}
