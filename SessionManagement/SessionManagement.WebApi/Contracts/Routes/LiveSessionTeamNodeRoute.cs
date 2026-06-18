namespace SessionManagement.WebApi.Contracts.Routes;

public readonly record struct LiveSessionTeamNodeRoute(Guid SessionId, Guid TeamId, Guid NodeId);
