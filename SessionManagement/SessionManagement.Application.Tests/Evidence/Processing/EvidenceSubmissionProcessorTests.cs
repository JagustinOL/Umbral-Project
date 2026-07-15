using FluentAssertions;
using SessionManagement.Application.Evidence.Processing;
using SessionManagement.Application.Evidence.Validation;
using SessionManagement.Application.Evidence.Validation.Handlers;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Entities;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.Evidence.Processing;

public sealed class EvidenceSubmissionProcessorTests
{
    private static readonly Guid NextNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Process_WhenLastNodeCompleted_MarksTeamCompleted_WithoutFinalizingSession()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100)],
            1.0m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();

        var rules = new[]
        {
            new NodeValidationRule(
                LiveSessionTestFactory.TriviaNodeId,
                1,
                NodeValidationType.Trivia,
                ["Bogota"])
        };

        var processor = CreateTriviaProcessor();
        var result = processor.Process(
            session,
            new EvidenceSubmissionRequest(
                LiveSessionTestFactory.DefaultTeamId,
                LiveSessionTestFactory.TriviaNodeId,
                "Bogota",
                rules,
                0));

        result.IsCorrect.Should().BeTrue();
        result.NodeCompleted.Should().BeTrue();
        result.NextNodeId.Should().BeNull();
        session.GetTeamParticipationStatus(LiveSessionTestFactory.DefaultTeamId)
            .Should().Be(TeamParticipationStatus.Completed);
        session.Status.Should().Be(LiveSessionStatus.Active);
    }

    [Fact]
    public void Process_WhenTriviaAnswerIsWrong_ClosesNodeWithZeroPointsAndAdvances()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [
                new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100),
                new AllowedNode(NextNodeId, "TreasureHunt", 150)
            ],
            1.0m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();

        var rules = new[]
        {
            new NodeValidationRule(
                LiveSessionTestFactory.TriviaNodeId,
                1,
                NodeValidationType.Trivia,
                ["Bogota"]),
            new NodeValidationRule(
                NextNodeId,
                2,
                NodeValidationType.TreasureHunt,
                ["CODE-123"])
        };

        var processor = CreateTriviaProcessor();
        var result = processor.Process(
            session,
            new EvidenceSubmissionRequest(
                LiveSessionTestFactory.DefaultTeamId,
                LiveSessionTestFactory.TriviaNodeId,
                "Medellin",
                rules,
                0));

        result.IsCorrect.Should().BeFalse();
        result.NodeCompleted.Should().BeTrue();
        result.AwardedPoints.Should().Be(0);
        result.NextNodeId.Should().Be(NextNodeId);

        var retry = () => processor.Process(
            session,
            new EvidenceSubmissionRequest(
                LiveSessionTestFactory.DefaultTeamId,
                LiveSessionTestFactory.TriviaNodeId,
                "Bogota",
                rules,
                0));
        retry.Should().Throw<SessionDomainException>();
    }

    private static TriviaEvidenceSubmissionProcessor CreateTriviaProcessor() =>
        new(new EvidenceValidatorService(
            new SessionActiveValidationHandler(),
            new TeamRegisteredValidationHandler(),
            new NodeAllowedValidationHandler(),
            new SequentialProgressValidationHandler(),
            new AnswerCorrectnessValidationHandler()));
}
