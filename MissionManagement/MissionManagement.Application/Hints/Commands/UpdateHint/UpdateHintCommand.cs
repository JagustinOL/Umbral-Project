using MediatR;

namespace MissionManagement.Application.Hints.Commands.UpdateHint;

public sealed record UpdateHintCommand(
    Guid MissionId,
    Guid NodeId,
    Guid HintId,
    string Content
) : IRequest;

