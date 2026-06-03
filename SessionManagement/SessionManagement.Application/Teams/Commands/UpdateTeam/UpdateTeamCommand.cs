using MediatR;

namespace SessionManagement.Application.Teams.Commands.UpdateTeam;

public sealed record UpdateTeamCommand(
    Guid TeamId,
    string NewName,
    Guid RequestorId
) : IRequest;
