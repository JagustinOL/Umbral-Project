using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.CoverageBoost;

public sealed class SessionDomainCoverageBoostTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Entity_OperatorsAndHashCode_CoverRemainingBranches()
    {
        var id = Guid.NewGuid();
        var a = TeamMember.Create(Guid.NewGuid(), "A");
        var b = TeamMember.Create(Guid.NewGuid(), "B");
        typeof(SessionManagement.Domain.Common.Entity).GetProperty(nameof(TeamMember.Id))!
            .SetValue(a, id);
        typeof(SessionManagement.Domain.Common.Entity).GetProperty(nameof(TeamMember.Id))!
            .SetValue(b, id);

        a.GetHashCode().Should().Be(b.GetHashCode());
        (a != b).Should().BeFalse();

        TeamMember? left = null;
        TeamMember? right = null;
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();

        a.Equals((object?)null).Should().BeFalse();
        a.Equals((object)a).Should().BeTrue();
    }

    [Fact]
    public void ClearDomainEvents_ClearsRaisedEvents()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        session.RegisterTeam(TeamId);
        session.DomainEvents.Should().NotBeEmpty();
        session.ClearDomainEvents();
        session.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void TeamParticipation_MarkExpelledAndGuards()
    {
        var actEmpty = () => TeamParticipation.Create(Guid.Empty);
        actEmpty.Should().Throw<ArgumentException>();

        var p = TeamParticipation.Create(TeamId);
        p.CanReceiveSupportMessage.Should().BeTrue();
        p.MarkExpelled();
        p.Status.Should().Be(TeamParticipationStatus.Expelled);
        p.CanReceiveSupportMessage.Should().BeFalse();

        var actComplete = () => p.MarkCompleted();
        actComplete.Should().Throw<SessionDomainException>();

        var completed = TeamParticipation.Create(Guid.NewGuid());
        completed.MarkCompleted();
        completed.MarkCompleted(); // idempotent
        completed.Status.Should().Be(TeamParticipationStatus.Completed);
        var actExpel = () => completed.MarkExpelled();
        actExpel.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void RejectJoinRequest_MarksRejected()
    {
        var op = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            Guid.NewGuid(), op,
            [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        session.SubmitJoinRequest(TeamId, session.JoinCode);
        session.RejectJoinRequest(TeamId, op);

        var req = session.JoinRequests.Single(r => r.TeamId == TeamId);
        req.Status.Should().Be(JoinRequestStatus.Rejected);
        req.ResolvedByOperatorId.Should().Be(op);

        var actAgain = () => session.RejectJoinRequest(TeamId, op);
        actAgain.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void Team_UnlockAndReleaseFromSession()
    {
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        var sessionId = Guid.NewGuid();
        team.AssignToSession(sessionId);
        team.Lock();
        team.Unlock();
        team.IsLocked.Should().BeFalse();
        team.CurrentSessionRef.Should().BeNull();

        team.AssignToSession(sessionId);
        team.Lock();
        team.ReleaseFromSession();
        team.IsLocked.Should().BeFalse();
        team.CurrentSessionRef.Should().BeNull();

        var before = team.Code;
        team.RegenerateCode();
        team.Code.Should().NotBe(before);
    }

    [Fact]
    public void Team_CreateAndUpdateName_WhenInvalid_Throws()
    {
        var act = () => Team.Create("N", Guid.NewGuid(), " ");
        act.Should().Throw<ArgumentException>();

        var leader = Guid.NewGuid();
        var team = Team.Create("Squad", leader, "Lead");
        var actName = () => team.UpdateName(" ", leader);
        actName.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void Evidence_RemarkPaths_Throw()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        var e = session.AcceptEvidence(TeamId, NodeId, "x", 0);
        session.MarkEvidenceAsValid(e.Id, publishScoreEvent: false);
        var actValidAgain = () => session.MarkEvidenceAsValid(e.Id);
        actValidAgain.Should().Throw<SessionDomainException>();

        var e2 = session.AcceptEvidence(TeamId, NodeId, "y", 1);
        session.MarkEvidenceAsInvalid(e2.Id, "bad");
        var actInvalidAgain = () => session.MarkEvidenceAsInvalid(e2.Id, "again");
        actInvalidAgain.Should().Throw<SessionDomainException>();

        var e3 = session.AcceptEvidence(TeamId, NodeId, "z", 2);
        var actBlank = () => session.MarkEvidenceAsInvalid(e3.Id, " ");
        actBlank.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AllowedNode_ParameterlessCtor_Defaults()
    {
        var node = new AllowedNode();
        node.NodeId.Should().Be(Guid.Empty);
        node.NodeType.Should().BeEmpty();
        node.BaseScore.Should().Be(0);
    }

    [Fact]
    public void ReleasedHint_Create_WhenInvalid_Throws()
    {
        var actTeam = () => ReleasedHint.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 0, true);
        var actHint = () => ReleasedHint.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 0, true);
        var actNode = () => ReleasedHint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 0, true);
        var actPenalty = () => ReleasedHint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1, true);

        actTeam.Should().Throw<ArgumentException>();
        actHint.Should().Throw<ArgumentException>();
        actNode.Should().Throw<ArgumentException>();
        actPenalty.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TeamMember_Create_WhenInvalid_Throws()
    {
        var actId = () => TeamMember.Create(Guid.Empty, "X");
        var actName = () => TeamMember.Create(Guid.NewGuid(), " ");
        actId.Should().Throw<ArgumentException>();
        actName.Should().Throw<ArgumentException>();
    }
}
