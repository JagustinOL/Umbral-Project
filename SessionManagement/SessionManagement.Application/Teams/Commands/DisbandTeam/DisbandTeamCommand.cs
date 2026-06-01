using MediatR;

namespace SessionManagement.Application.Teams.Commands.DisbandTeam;

public sealed record DisbandTeamCommand(
    Guid TeamId,
    Guid RequestorId
) : IRequest;
