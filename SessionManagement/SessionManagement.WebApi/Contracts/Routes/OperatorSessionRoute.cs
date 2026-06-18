namespace SessionManagement.WebApi.Contracts.Routes;

public readonly record struct OperatorSessionRoute(Guid OperatorId, Guid SessionId);
