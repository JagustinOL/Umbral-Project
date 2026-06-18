namespace SessionManagement.WebApi.Contracts.Routes;

public sealed record LiveSessionTeamNodeRoute(Guid SessionId, Guid TeamId, Guid NodeId);
