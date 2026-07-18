using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.ReleaseManualHint;

public sealed record ReleaseManualHintCommand(
    Guid OperatorId,
    Guid SessionId,
    Guid TeamId,
    Guid HintId) : IRequest;
