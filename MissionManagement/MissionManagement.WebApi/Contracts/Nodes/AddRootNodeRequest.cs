namespace MissionManagement.WebApi.Contracts.Nodes;

public sealed record AddRootNodeRequest(
    string Title,
    string Description,
    int ExecutionOrder
);

