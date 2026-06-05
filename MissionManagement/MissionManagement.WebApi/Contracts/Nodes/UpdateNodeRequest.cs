namespace MissionManagement.WebApi.Contracts.Nodes;

public sealed record UpdateNodeRequest(
    string Title,
    string Description
);

