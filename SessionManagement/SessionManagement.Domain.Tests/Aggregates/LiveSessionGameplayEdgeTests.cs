using FluentAssertions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Domain.Tests.Aggregates;

public sealed class LiveSessionGameplayEdgeTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TriviaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TreasureId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static LiveSession BuildActiveWithRules(out IReadOnlyList<NodeValidationRule> rules)
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(TriviaId, "Trivia", 100), new AllowedNode(TreasureId, "TreasureHunt", 150)], 1m);
        session.RegisterTeam(TeamId);
        session.BeginPreparation();
        session.Start();
        rules =
        [
            new NodeValidationRule(TriviaId, 1, NodeValidationType.Trivia, ["Bogota"]),
            new NodeValidationRule(TreasureId, 2, NodeValidationType.TreasureHunt, ["CODE-123"])
        ];
        return session;
    }

    [Fact]
    public void SubmitTrivia_WhenWrongNodeType_Throws()
    {
        var session = BuildActiveWithRules(out var rules);
        var act = () => session.SubmitTreasureHuntCode(TeamId, TriviaId, "CODE", rules);
        act.Should().Throw<SessionDomainException>().WithMessage("*Tipo de validación*");
    }

    [Fact]
    public void SubmitTrivia_WhenEmptyPayload_Throws()
    {
        var session = BuildActiveWithRules(out var rules);
        var act = () => session.SubmitTriviaAnswer(TeamId, TriviaId, "  ", 0, rules);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SubmitTrivia_WhenAllNodesDone_Throws()
    {
        var session = BuildActiveWithRules(out var rules);
        session.SubmitTriviaAnswer(TeamId, TriviaId, "Bogota", 0, rules);
        session.SubmitTreasureHuntCode(TeamId, TreasureId, "CODE-123", rules);
        var act = () => session.SubmitTriviaAnswer(TeamId, TriviaId, "Bogota", 0, rules);
        act.Should().Throw<SessionDomainException>().WithMessage("*Completed*");
    }

    [Fact]
    public void ApplyManualPenalty_WhenInvalidReason_Throws()
    {
        var session = BuildActiveWithRules(out _);
        var act = () => session.ApplyManualPenalty(TeamId, session.OperatorRef, 10, "  ");
        act.Should().Throw<SessionDomainException>().WithMessage("*RB-06*");
    }

    [Fact]
    public void TransitionTo_WhenInvalid_ThrowsRb09()
    {
        var session = LiveSession.CreateForMission(Guid.NewGuid(), Guid.NewGuid(),
            [new AllowedNode(TriviaId, "Trivia", 10)], 1m);
        var act = () => session.Finalize();
        act.Should().Throw<SessionDomainException>().WithMessage("*RB-09*");
    }
}
