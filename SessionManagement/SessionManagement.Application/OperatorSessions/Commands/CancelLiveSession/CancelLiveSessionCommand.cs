using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.CancelLiveSession;

public sealed record CancelLiveSessionCommand(Guid OperatorId, Guid SessionId) : IRequest;
