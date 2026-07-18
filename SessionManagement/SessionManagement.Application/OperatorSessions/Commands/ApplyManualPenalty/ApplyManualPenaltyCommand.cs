using MediatR;

namespace SessionManagement.Application.OperatorSessions.Commands.ApplyManualPenalty;

public sealed record ApplyManualPenaltyCommand(
    Guid OperatorId,
    Guid SessionId,
    Guid TeamId,
    int Points,
    string Reason) : IRequest;
