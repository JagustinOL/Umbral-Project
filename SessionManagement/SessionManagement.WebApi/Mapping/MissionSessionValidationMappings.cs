using SessionManagement.Application.Missions.Queries.MissionHasOpenSessions;
using SessionManagement.WebApi.Contracts.Routes;

namespace SessionManagement.WebApi.Mapping;

public static class MissionSessionValidationMappings
{
    public static MissionHasOpenSessionsQuery ToQuery(this MissionRoute route) =>
        new(route.MissionId);
}
