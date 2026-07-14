using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.SendSupportMessage;

public sealed record SendSupportMessageCommand(
    Guid OperatorId,
    Guid SessionId,
    Guid TeamId,
    string Message) : IRequest;
