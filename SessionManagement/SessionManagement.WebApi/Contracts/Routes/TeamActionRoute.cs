using Microsoft.AspNetCore.Mvc;

namespace SessionManagement.WebApi.Contracts.Routes;

public sealed record TeamActionRoute(
    [FromRoute(Name = "teamId")] Guid TeamId,
    [FromQuery] Guid RequestorId);
