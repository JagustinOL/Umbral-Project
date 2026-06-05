using MediatR;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Application.Nodes.Commands.UpdateTreasureHuntNode;

public sealed record UpdateTreasureHuntNodeCommand(
    Guid MissionId,
    Guid NodeId,
    string Instructions,
    string SecretCode,
    GpsCoordinate Destination
) : IRequest;

