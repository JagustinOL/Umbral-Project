using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.LiveSessions.Queries.GetTeamCurrentNodeContent;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Queries.GetTeamCurrentNodeContent;

public sealed class GetTeamCurrentNodeContentHandlerTests
{
    [Fact]
    public async Task Handle_WhenSessionMissing_ThrowsNotFound()
    {
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveSession?)null);

        var handler = new GetTeamCurrentNodeContentHandler(
            repo.Object, new Mock<IMissionIntegrationService>().Object);

        var act = () => handler.Handle(
            new GetTeamCurrentNodeContentQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ReturnsTriviaContentWithQuestionIndex()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());
        missions.Setup(m => m.GetNodePlayerContentAsync(
                session.MissionRef, LiveSessionTestFactory.TriviaNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerNodeContentData(
                LiveSessionTestFactory.TriviaNodeId,
                "Trivia",
                [new PlayerTriviaQuestionData("¿Capital?", ["Bogotá", "Cali"])],
                null,
                null));

        var handler = new GetTeamCurrentNodeContentHandler(repo.Object, missions.Object);

        var result = await handler.Handle(
            new GetTeamCurrentNodeContentQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        result.NodeId.Should().Be(LiveSessionTestFactory.TriviaNodeId);
        result.NodeType.Should().Be("Trivia");
        result.TotalQuestions.Should().Be(1);
        result.CurrentQuestionIndex.Should().Be(0);
        result.Questions.Should().ContainSingle(q => q.Prompt.Contains("Capital"));
    }

    [Fact]
    public async Task Handle_WhenTeamCompletedAllNodes_ThrowsNotFound()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TriviaNodeId, "Trivia", 100)],
            1m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();
        var evidence = session.AcceptEvidence(
            LiveSessionTestFactory.DefaultTeamId, LiveSessionTestFactory.TriviaNodeId, "Bogota", 0);
        session.MarkEvidenceAsValid(evidence.Id);
        session.MarkTeamCompleted(LiveSessionTestFactory.DefaultTeamId);

        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(LiveSessionTestFactory.TriviaNodeId, "Trivia", 1, 100, ["Bogota"])
            ]);

        var handler = new GetTeamCurrentNodeContentHandler(repo.Object, missions.Object);

        var act = () => handler.Handle(
            new GetTeamCurrentNodeContentQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*completó*");
    }

    [Fact]
    public async Task Handle_WhenTreasureHunt_UsesInstructionsAndSingleQuestion()
    {
        var session = LiveSession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new AllowedNode(LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 150)],
            1m);
        session.RegisterTeam(LiveSessionTestFactory.DefaultTeamId);
        session.BeginPreparation();
        session.Start();

        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(
                    LiveSessionTestFactory.TreasureNodeId, "TreasureHunt", 1, 150, ["CODE"])
            ]);
        missions.Setup(m => m.GetNodePlayerContentAsync(
                session.MissionRef, LiveSessionTestFactory.TreasureNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerNodeContentData(
                LiveSessionTestFactory.TreasureNodeId,
                "TreasureHunt",
                null,
                "Busca el monumento",
                new GpsCoordinateData(4.6, -74.0)));

        var handler = new GetTeamCurrentNodeContentHandler(repo.Object, missions.Object);

        var result = await handler.Handle(
            new GetTeamCurrentNodeContentQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        result.Instructions.Should().Be("Busca el monumento");
        result.TotalQuestions.Should().Be(1);
        result.CurrentQuestionIndex.Should().Be(0);
        result.Destination.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenUnsupportedNodeType_Throws()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(r => r.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var missions = new Mock<IMissionIntegrationService>();
        missions.Setup(m => m.GetNodeValidationDataAsync(session.MissionRef, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new MissionNodeValidationData(LiveSessionTestFactory.TriviaNodeId, "Stage", 1, 0, [])
            ]);

        var handler = new GetTeamCurrentNodeContentHandler(repo.Object, missions.Object);

        var act = () => handler.Handle(
            new GetTeamCurrentNodeContentQuery(session.Id, LiveSessionTestFactory.DefaultTeamId),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no soportado*");
    }
}
