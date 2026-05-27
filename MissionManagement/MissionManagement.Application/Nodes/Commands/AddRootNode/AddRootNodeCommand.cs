using MediatR;

namespace MissionManagement.Application.Nodes.Commands.AddRootNode;

public sealed record AddRootNodeCommand(
    Guid MissionId,
    string Title,
    string Description,
    int ExecutionOrder
) : IRequest<Guid>;

