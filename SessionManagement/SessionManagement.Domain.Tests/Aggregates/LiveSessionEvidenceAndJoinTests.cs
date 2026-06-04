using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionEvidenceAndJoinTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void JoinTeam_WhenJoinCodeMatches_RegistersTeam()
    {
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 10)], 1m);
        var newTeam = Guid.NewGuid();

        session.JoinTeam(newTeam, session.JoinCode);

        session.RegisteredTeamIds.Should().Contain(newTeam);
    }

    [Fact]
    public void JoinTeam_WhenJoinCodeInvalid_Throws()
    {
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 10)], 1m);

        var act = () => session.JoinTeam(Guid.NewGuid(), "WRONG");

        act.Should().Throw<SessionDomainException>();
    }

    [Fact]
    public void AcceptEvidence_MarkValidAndInvalid_Works()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 100)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();

        var evidence = session.AcceptEvidence(TeamId, NodeId, "payload");
        session.MarkEvidenceAsValid(evidence.Id);
        evidence.IsValid.Should().BeTrue();
        session.DomainEvents.Should().Contain(e => e.GetType().Name == "EvidenceValidatedEvent");

        var evidence2 = session.AcceptEvidence(TeamId, NodeId, "bad");
        session.MarkEvidenceAsInvalid(evidence2.Id, "incorrect");
        evidence2.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BeginPreparation_FromPending_ChangesStatus()
    {
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(NodeId, "Trivia", 10)], 1m);
        session.RegisterTeam(TeamId);

        session.BeginPreparation();

        session.Status.Should().Be(LiveSessionStatus.Preparation);
    }
}
