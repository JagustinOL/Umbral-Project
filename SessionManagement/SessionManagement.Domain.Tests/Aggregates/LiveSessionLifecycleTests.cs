using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionLifecycleTests
{
    [Fact]
    public void StartSession_WithoutTeams_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = LiveSession.CreateForMission(
            missionRef: Guid.NewGuid(),
            operatorRef: Guid.NewGuid(),
            allowedNodes:
            [
                new AllowedNode(Guid.NewGuid(), "Trivia", 100)
            ],
            difficultyMultiplier: 1.0m);

        session.BeginPreparation();

        // Act
        var act = () => session.StartSession();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RN-15*");
    }

    [Fact]
    public void StartSession_WithAtLeastOneTeam_ChangesStatusToActive()
    {
        // Arrange
        var session = LiveSession.CreateForMission(
            missionRef: Guid.NewGuid(),
            operatorRef: Guid.NewGuid(),
            allowedNodes:
            [
                new AllowedNode(Guid.NewGuid(), "Trivia", 100)
            ],
            difficultyMultiplier: 1.0m);

        session.RegisterTeam(Guid.NewGuid());
        session.BeginPreparation();

        // Act
        session.StartSession();

        // Assert
        session.Status.Should().Be(LiveSessionStatus.Active);
        session.StartedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void CompletingLastNode_MarksTeamCompleted_ButKeepsSessionActiveUntilOperatorFinalizes()
    {
        var nodeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var session = LiveSession.CreateForMission(
            missionRef: Guid.NewGuid(),
            operatorRef: Guid.NewGuid(),
            allowedNodes: [new AllowedNode(nodeId, "Trivia", 100)],
            difficultyMultiplier: 1.0m);

        session.RegisterTeam(teamId);
        session.BeginPreparation();
        session.StartSession();

        var rules = new[]
        {
            new NodeValidationRule(nodeId, 1, NodeValidationType.Trivia, ["Bogota"])
        };

        session.SubmitTriviaAnswer(teamId, nodeId, "Bogota", 0, rules);

        session.GetTeamParticipationStatus(teamId).Should().Be(TeamParticipationStatus.Completed);
        session.Status.Should().Be(LiveSessionStatus.Active);

        session.Finalize();
        session.Status.Should().Be(LiveSessionStatus.Finalized);
        session.FinalizedAtUtc.Should().NotBeNull();
    }
}

