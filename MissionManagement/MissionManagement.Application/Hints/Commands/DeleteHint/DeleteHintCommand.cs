using MediatR;

namespace MissionManagement.Application.Hints.Commands.DeleteHint;

public sealed record DeleteHintCommand(
    Guid MissionId,
    Guid NodeId,
    Guid HintId
) : IRequest;

