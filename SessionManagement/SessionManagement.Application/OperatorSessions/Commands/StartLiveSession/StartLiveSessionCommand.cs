using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.StartLiveSession;

public sealed record StartLiveSessionCommand(
    Guid OperatorId,
    Guid SessionId
) : IRequest;

