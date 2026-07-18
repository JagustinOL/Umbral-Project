using MediatR;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Application.Nodes.Commands.AddTreasureHuntNode;

public sealed record AddTreasureHuntNodeCommand(
    Guid MissionId,
    Guid ParentNodeId,
    string Instructions,
    string SecretCode,
    GpsCoordinate Destination,
    int ExecutionOrder,
    int BaseScore
) : IRequest<Guid>;
