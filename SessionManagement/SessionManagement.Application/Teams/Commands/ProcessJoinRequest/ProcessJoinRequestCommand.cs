using MediatR;

namespace SessionManagement.Application.Teams.Commands.ProcessJoinRequest;

public sealed record ProcessJoinRequestCommand(
    Guid TeamId,
    Guid RequestId,
    bool IsApproved,
    Guid RequestorId
) : IRequest;
