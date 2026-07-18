using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionParticipationTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TriviaNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TreasureNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void SubmitTriviaAnswer_ShouldReject_WhenSessionIsNotActive_RN03()
    {
        // Arrange
        var session = BuildSession(started: false);
        var rules = BuildRules();

        // Act
        var act = () => session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Bogota", 0, rules);

        // Assert
        act.Should().Throw<SessionDomainException>()
            .WithMessage("*debe estar Active*");
    }

    [Fact]
    public void SubmitTriviaAnswer_ShouldReject_WhenNodeAlreadyClosed_RN04()
    {
        // Arrange
        var session = BuildSession(started: true);
        var rules = BuildRules();

        session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Bogota", 0, rules);

        // Act
        var act = () => session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Bogota", 0, rules);

        // Assert
        act.Should().Throw<SessionDomainException>()
            .WithMessage("*ya está cerrada*");
    }

    [Fact]
    public void SubmitTriviaAnswer_ShouldReject_WhenNodeIsOutOfSequence_RN11()
    {
        // Arrange
        var session = BuildSession(started: true);
        var rules = BuildRules();

        // Act
        var act = () => session.SubmitTreasureHuntCode(TeamId, TreasureNodeId, "CODE-123", rules);

        // Assert
        act.Should().Throw<SessionDomainException>()
            .WithMessage("*Progresión secuencial*");
    }

    [Fact]
    public void Submissions_ShouldValidateTriviaAndTreasureHunt_RN12()
    {
        // Arrange
        var session = BuildSession(started: true);
        var rules = BuildRules();

        // Act
        var triviaResult = session.SubmitTriviaAnswer(TeamId, TriviaNodeId, "Bogota", 0, rules);
        var treasureResult = session.SubmitTreasureHuntCode(TeamId, TreasureNodeId, "bad-code", rules);

        // Assert
        triviaResult.IsCorrect.Should().BeTrue();
        triviaResult.NextNodeId.Should().Be(TreasureNodeId);
        treasureResult.IsCorrect.Should().BeFalse();
        treasureResult.NextNodeId.Should().Be(TreasureNodeId);
    }

    private static LiveSession BuildSession(bool started)
    {
        var session = LiveSession.Create(
            missionRef: Guid.NewGuid(),
            operatorRef: Guid.NewGuid(),
            allowedNodes:
            [
                new AllowedNode(TriviaNodeId, "Trivia", 100),
                new AllowedNode(TreasureNodeId, "TreasureHunt", 150)
            ],
            difficultyMultiplier: 1.0m);

        session.RegisterTeam(TeamId);
        if (started)
        {
            session.BeginPreparation();
            session.Start();
        }

        return session;
    }

    private static IReadOnlyList<NodeValidationRule> BuildRules() =>
    [
        new NodeValidationRule(TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota"]),
        new NodeValidationRule(TreasureNodeId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
    ];
}

