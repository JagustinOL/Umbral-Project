using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Exceptions;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class TeamTests
{
    [Fact]
    public void Create_WithValidData_AddsLeaderMember()
    {
        // Arrange
        var creatorId = Guid.NewGuid();

        // Act
        var team = Team.Create("Los Exploradores", creatorId, "Ana");

        // Assert
        team.Members.Should().HaveCount(1);
        team.Members.Single().PlayerRef.Should().Be(creatorId);
        team.Members.Single().Role.Should().Be(TeamMemberRole.Leader);
    }

    [Fact]
    public void SubmitAndApproveJoinRequest_WhenUnlocked_AddsMember()
    {
        // Arrange
        var team = Team.Create("Alpha", Guid.NewGuid(), "Lider");
        var playerId = Guid.NewGuid();

        // Act
        team.SubmitJoinRequest(playerId, "Jugador2");
        var request = team.JoinRequests.Single();
        team.ProcessJoinRequest(request.Id, isApproved: true);

        // Assert
        team.Members.Should().Contain(x => x.PlayerRef == playerId);
        request.Status.Should().Be(JoinRequestStatus.Approved);
    }

    [Fact]
    public void RemoveMember_WhenLeaderLeaves_DelegatesLeadership()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Bravo", leaderId, "Lider");
        var nextMember = TeamMember.Create(Guid.NewGuid(), "M2");
        team.AddMember(nextMember);

        // Act
        team.RemoveMember(leaderId, leaderId);

        // Assert
        team.Members.Should().HaveCount(1);
        team.Members.Single().Role.Should().Be(TeamMemberRole.Leader);
    }

    [Fact]
    public void UpdateName_WhenLocked_ThrowsDomainException()
    {
        // Arrange
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Charlie", leaderId, "Lider");
        team.Lock();

        // Act
        var act = () => team.UpdateName("Delta", leaderId);

        // Assert
        act.Should().Throw<SessionDomainException>();
    }
}
