namespace SessionManagement.WebApi.Contracts.Routes;

public readonly record struct TeamJoinRequestRoute(Guid TeamId, Guid RequestId);
