using SessionManagement.Application.Teams.Commands.CreateTeam;
using SessionManagement.Application.Teams.Commands.DisbandTeam;
using SessionManagement.Application.Teams.Commands.ProcessJoinRequest;
using SessionManagement.Application.Teams.Commands.RemoveMember;
using SessionManagement.Application.Teams.Commands.SubmitJoinRequest;
using SessionManagement.Application.Teams.Commands.UpdateTeam;
using SessionManagement.Application.Teams.Queries.GetPendingRequests;
using SessionManagement.Application.Teams.Queries.GetTeamById;
using SessionManagement.WebApi.Contracts.Routes;
using SessionManagement.WebApi.Contracts.Teams;

namespace SessionManagement.WebApi.Mapping;

public static class TeamMappings
{
    public static CreateTeamCommand ToCommand(this CreateTeamRequest body) =>
        new(
            Name: body.Name,
            CreatorId: body.CreatorId,
            CreatorDisplayName: body.CreatorDisplayName);

    public static GetTeamByIdQuery ToQuery(this TeamRoute route) =>
        new(route.TeamId);

    public static UpdateTeamCommand ToCommand(this UpdateTeamRequest body, TeamRoute route) =>
        new(TeamId: route.TeamId, NewName: body.NewName, RequestorId: body.RequestorId);

    public static DisbandTeamCommand ToCommand(this TeamActionRoute route) =>
        new(TeamId: route.TeamId, RequestorId: route.RequestorId);

    public static SubmitJoinRequestCommand ToCommand(this SubmitJoinRequestRequest body) =>
        new(
            TeamCode: body.TeamCode,
            PlayerId: body.PlayerRef,
            DisplayName: body.DisplayName);

    public static GetPendingRequestsQuery ToQuery(this TeamActionRoute route) =>
        new(TeamId: route.TeamId, RequestorId: route.RequestorId);

    public static ProcessJoinRequestCommand ToCommand(
        this ProcessJoinRequestRequest body,
        TeamJoinRequestRoute route) =>
        new(
            TeamId: route.TeamId,
            RequestId: route.RequestId,
            IsApproved: body.Approve,
            RequestorId: body.RequestorId);

    public static RemoveMemberCommand ToCommand(this TeamMemberActionRoute route) =>
        new(TeamId: route.TeamId, PlayerId: route.PlayerId, RequestorId: route.RequestorId);
}
