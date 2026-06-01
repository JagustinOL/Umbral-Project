using MediatR;

namespace SessionManagement.Application.Teams.Commands.CreateTeam;

public sealed record CreateTeamCommand(
    string Name,
    Guid CreatorId,
    string? CreatorDisplayName
) : IRequest<Guid>;
