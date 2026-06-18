namespace SessionManagement.WebApi.Contracts.Routes;

public readonly record struct LiveSessionTeamRoute(Guid SessionId, Guid TeamId);
