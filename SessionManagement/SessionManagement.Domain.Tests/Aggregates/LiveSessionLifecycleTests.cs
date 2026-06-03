using FluentAssertions;
using SessionManagement.Domain.Aggregates;
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
}

