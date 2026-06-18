namespace SessionManagement.WebApi.Contracts.Routes;

public readonly record struct TeamActionRoute(Guid TeamId, Guid RequestorId);
