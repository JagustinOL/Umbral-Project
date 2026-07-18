using MediatR;

namespace MissionManagement.Application.Hints.Commands.AddHint;

public sealed record AddHintCommand(
    Guid MissionId,
    Guid NodeId,
    string Content,
    int PenaltyPoints
) : IRequest<Guid>;
