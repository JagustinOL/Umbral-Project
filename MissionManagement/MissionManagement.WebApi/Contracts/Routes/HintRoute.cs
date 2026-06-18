namespace MissionManagement.WebApi.Contracts.Routes;

public sealed record HintRoute(Guid MissionId, Guid NodeId, Guid HintId);
