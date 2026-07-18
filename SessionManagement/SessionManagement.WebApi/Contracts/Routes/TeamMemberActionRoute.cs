using Microsoft.AspNetCore.Mvc;

namespace SessionManagement.WebApi.Contracts.Routes;

public sealed record TeamMemberActionRoute(
    [FromRoute(Name = "teamId")] Guid TeamId,
    [FromRoute(Name = "playerId")] Guid PlayerId,
    [FromQuery] Guid RequestorId);
