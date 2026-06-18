namespace MissionManagement.WebApi.Contracts.Routes;

public readonly record struct HintRoute(Guid MissionId, Guid NodeId, Guid HintId);
