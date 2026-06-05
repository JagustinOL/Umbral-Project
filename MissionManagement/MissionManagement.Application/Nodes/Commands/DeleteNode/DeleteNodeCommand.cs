using MediatR;

namespace MissionManagement.Application.Nodes.Commands.DeleteNode;

public sealed record DeleteNodeCommand(
    Guid MissionId,
    Guid NodeId
) : IRequest;

