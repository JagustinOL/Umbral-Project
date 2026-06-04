using FluentAssertions;
using Moq;
using SessionManagement.Application.Teams.Queries.GetPlayerTeamMembership;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Queries.GetPlayerTeamMembership;

public sealed class GetPlayerTeamMembershipHandlerTests
{
    [Fact]
    public async Task Handle_WhenPlayerIsMember_ReturnsMembership()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Equipo Alfa", leaderId, "Leader");
        var teamRepositoryMock = new Mock<ITeamRepository>();

        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(leaderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamWithPendingJoinRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var handler = new GetPlayerTeamMembershipHandler(teamRepositoryMock.Object);
        var result = await handler.Handle(new GetPlayerTeamMembershipQuery(leaderId), CancellationToken.None);

        result.IsMember.Should().BeTrue();
        result.TeamId.Should().Be(team.Id);
        result.Role.Should().Be("Leader");
        result.HasPendingJoinRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPlayerHasPendingRequest_ReturnsPendingTeam()
    {
        var applicantId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Equipo Beta", leaderId, "Leader");
        team.SubmitJoinRequest(applicantId, "Applicant");

        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamWithPendingJoinRequestAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        var handler = new GetPlayerTeamMembershipHandler(teamRepositoryMock.Object);
        var result = await handler.Handle(new GetPlayerTeamMembershipQuery(applicantId), CancellationToken.None);

        result.IsMember.Should().BeFalse();
        result.HasPendingJoinRequest.Should().BeTrue();
        result.PendingTeamId.Should().Be(team.Id);
    }

    [Fact]
    public async Task Handle_WhenPlayerHasNoTeam_ReturnsEmptyMembership()
    {
        var playerId = Guid.NewGuid();
        var teamRepositoryMock = new Mock<ITeamRepository>();
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamByPlayerRefAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);
        teamRepositoryMock
            .Setup(x => x.GetActiveTeamWithPendingJoinRequestAsync(playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var handler = new GetPlayerTeamMembershipHandler(teamRepositoryMock.Object);
        var result = await handler.Handle(new GetPlayerTeamMembershipQuery(playerId), CancellationToken.None);

        result.IsMember.Should().BeFalse();
        result.TeamId.Should().BeNull();
        result.HasPendingJoinRequest.Should().BeFalse();
    }
}
