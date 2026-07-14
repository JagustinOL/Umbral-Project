using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.ToggleSessionPause;

public sealed record ToggleSessionPauseCommand(
    Guid OperatorId,
    Guid SessionId,
    string? Reason = null) : IRequest<string>;
