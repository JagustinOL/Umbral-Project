using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.FinalizeLiveSession;

public sealed record FinalizeLiveSessionCommand(Guid OperatorId, Guid SessionId) : IRequest;
