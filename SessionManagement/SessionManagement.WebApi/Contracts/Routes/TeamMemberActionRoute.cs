namespace SessionManagement.WebApi.Contracts.Routes;

public readonly record struct TeamMemberActionRoute(Guid TeamId, Guid PlayerId, Guid RequestorId);
