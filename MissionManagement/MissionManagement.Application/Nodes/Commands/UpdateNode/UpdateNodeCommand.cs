using MediatR;

namespace MissionManagement.Application.Nodes.Commands.UpdateNode;

public sealed record UpdateNodeCommand(
    Guid MissionId,
    Guid NodeId,
    string Title,
    string Description
) : IRequest;

