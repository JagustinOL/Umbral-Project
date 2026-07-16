using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.ProcessSessionJoinRequest;

public sealed record ProcessSessionJoinRequestCommand(
    Guid OperatorId,
    Guid SessionId,
    Guid TeamId,
    bool Approve) : IRequest;
