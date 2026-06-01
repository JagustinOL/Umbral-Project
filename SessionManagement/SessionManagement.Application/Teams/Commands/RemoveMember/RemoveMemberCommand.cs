using MediatR;

namespace SessionManagement.Application.Teams.Commands.RemoveMember;

public sealed record RemoveMemberCommand(
    Guid TeamId,
    Guid PlayerId,
    Guid RequestorId
) : IRequest;
