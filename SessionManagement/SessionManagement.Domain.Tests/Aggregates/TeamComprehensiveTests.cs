using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Exceptions;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class TeamComprehensiveTests
{
    private static (Team team, Guid leaderId) CreateTeam()
    {
        var leaderId = Guid.NewGuid();
        return (Team.Create("Squad", leaderId, "Leader"), leaderId);
    }

    [Fact]
    public void Create_WhenInvalidArgs_Throws()
    {
        var act1 = () => Team.Create(" ", Guid.NewGuid(), "L");
        act1.Should().Throw<ArgumentException>();
        var act2 = () => Team.Create("N", Guid.Empty, "L");
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMember_WhenFullOrDuplicate_Throws()
    {
        var (team, leaderId) = CreateTeam();
        for (var i = 0; i < 3; i++)
            team.AddMember(TeamMember.Create(Guid.NewGuid(), $"M{i}"));
        var full = () => team.AddMember(TeamMember.Create(Guid.NewGuid(), "Extra"));
        full.Should().Throw<SessionDomainException>().WithMessage("*máximo*");

        var dup = () => team.AddMember(TeamMember.Create(leaderId, "Dup"));
        dup.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void ProcessJoinRequest_WhenRejected_SetsStatus()
    {
        var (team, _) = CreateTeam();
        var playerId = Guid.NewGuid();
        team.SubmitJoinRequest(playerId, "P");
        var requestId = team.JoinRequests.Single().Id;
        team.ProcessJoinRequest(requestId, isApproved: false);
        team.JoinRequests.Single().Status.Should().Be(JoinRequestStatus.Rejected);
        team.Members.Should().HaveCount(1);
    }

    [Fact]
    public void ProcessJoinRequest_WhenAlreadyProcessed_Throws()
    {
        var (team, _) = CreateTeam();
        team.SubmitJoinRequest(Guid.NewGuid(), "P");
        var requestId = team.JoinRequests.Single().Id;
        team.ProcessJoinRequest(requestId, false);
        var act = () => team.ProcessJoinRequest(requestId, true);
        act.Should().Throw<SessionDomainException>().WithMessage("*procesada*");
    }

    [Fact]
    public void RemoveMember_WhenNotLeaderRemovesOther_Throws()
    {
        var (team, leaderId) = CreateTeam();
        var memberId = Guid.NewGuid();
        team.AddMember(TeamMember.Create(memberId, "M"));
        var act = () => team.RemoveMember(leaderId, memberId);
        act.Should().Throw<SessionDomainException>().WithMessage("*líder*");
    }

    [Fact]
    public void RemoveMember_WhenLastMember_Disbands()
    {
        var (team, leaderId) = CreateTeam();
        team.RemoveMember(leaderId, leaderId);
        team.IsDisbanded.Should().BeTrue();
    }

    [Fact]
    public void Disband_WhenNotLeader_Throws()
    {
        var (team, _) = CreateTeam();
        var memberId = Guid.NewGuid();
        team.AddMember(TeamMember.Create(memberId, "M"));
        var act = () => team.Disband(memberId);
        act.Should().Throw<SessionDomainException>().WithMessage("*líder*");
    }

    [Fact]
    public void AssignToSession_WhenSameSession_IsIdempotent()
    {
        var (team, _) = CreateTeam();
        var sessionId = Guid.NewGuid();
        team.AssignToSession(sessionId);
        team.AssignToSession(sessionId);
        team.CurrentSessionRef.Should().Be(sessionId);
    }

    [Fact]
    public void Lock_WhenAlreadyLocked_Throws()
    {
        var (team, _) = CreateTeam();
        team.Lock();
        var act = () => team.Lock();
        act.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void Unlock_WhenNotLocked_Throws()
    {
        var (team, _) = CreateTeam();
        var act = () => team.Unlock();
        act.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void RegenerateCode_WhenDisbanded_Throws()
    {
        var (team, leaderId) = CreateTeam();
        team.Disband(leaderId);
        var act = () => team.RegenerateCode();
        act.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void SubmitJoinRequest_WhenDuplicatePending_Throws()
    {
        var (team, _) = CreateTeam();
        var playerId = Guid.NewGuid();
        team.SubmitJoinRequest(playerId, "P");
        var act = () => team.SubmitJoinRequest(playerId, "P");
        act.Should().Throw<SessionDomainException>().WithMessage("*pendiente*");
    }
}
