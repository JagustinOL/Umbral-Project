using ScoringAudit.Application.Queries;
using ScoringAudit.WebApi.Contracts.Routes;

namespace ScoringAudit.WebApi.Mapping;

public static class RankingMappings
{
    public static GetSessionRankingQuery ToQuery(this SessionRoute route) =>
        new(route.SessionId);
}
