using FluentAssertions;
using FluentValidation.TestHelper;
using SessionManagement.Application.Dtos;
using SessionManagement.Application.Evidence;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Exceptions;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.CoverageGaps;

public sealed class CoverageGapFillTests
{
    [Fact]
    public void CreateLiveSessionCommandValidator_WhenEmptyIds_Fails()
    {
        var validator = new CreateLiveSessionCommandValidator();
        var result = validator.TestValidate(new CreateLiveSessionCommand(Guid.Empty, Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.OperatorId);
        result.ShouldHaveValidationErrorFor(x => x.MissionId);
    }

    [Fact]
    public void CreateLiveSessionCommandValidator_WhenValid_Passes()
    {
        var validator = new CreateLiveSessionCommandValidator();
        var result = validator.TestValidate(new CreateLiveSessionCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SessionDomainException_WithInner_PreservesMessage()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SessionDomainException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void OperatorReleasedHintSummaryDto_CanBeConstructed()
    {
        var dto = new OperatorReleasedHintSummaryDto(
            Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow, true, "Trivia", "prompt");
        dto.PenaltyPoints.Should().Be(5);
        dto.WasManualRelease.Should().BeTrue();
        dto.NodePrompt.Should().Be("prompt");
    }

    [Fact]
    public void NodeProgressHelper_TriviaFailedAttempt_IsCompleted()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var evidence = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "wrong", 0);
        session.MarkEvidenceAsInvalid(evidence.Id, "bad");

        var rule = new NodeValidationRule(
            LiveSessionTestFactory.TriviaNodeId, 1, NodeValidationType.Trivia, ["Bogota", "Medellin"]);

        NodeProgressHelper.IsNodeCompleted(session, LiveSessionTestFactory.DefaultTeamId, rule)
            .Should().BeTrue();

        var completed = NodeProgressHelper.GetCompletedNodeIds(
            session, LiveSessionTestFactory.DefaultTeamId, [rule]);
        completed.Should().Contain(LiveSessionTestFactory.TriviaNodeId);
    }

    [Fact]
    public void NodeProgressHelper_GetNextQuestionIndex_WhenAllAnswered_ReturnsTotal()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var e0 = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "a", 0);
        session.MarkEvidenceAsValid(e0.Id, publishScoreEvent: false);
        var e1 = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "b", 1);
        session.MarkEvidenceAsValid(e1.Id, publishScoreEvent: false);

        NodeProgressHelper.GetNextQuestionIndex(
                session, LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, 2)
            .Should().Be(2);
    }
}
