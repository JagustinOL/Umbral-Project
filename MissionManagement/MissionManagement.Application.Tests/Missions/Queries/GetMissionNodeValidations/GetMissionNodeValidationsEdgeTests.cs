using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Queries.GetMissionNodeValidations;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Queries.GetMissionNodeValidations;

public sealed class GetMissionNodeValidationsEdgeTests
{
    [Fact]
    public async Task Handle_WhenTreasureWithoutSecret_ThrowsConflict()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var thId = mission.AddTreasureHuntNode(stage.Id, "Go", "SECRET", new GpsCoordinate(1, 2), 1, baseScore: 100);
        var thNode = mission.FindNodeById(thId)!;
        typeof(MissionManagement.Domain.Entities.MissionNode)
            .GetProperty(nameof(MissionManagement.Domain.Entities.MissionNode.SecretCode))!
            .SetValue(thNode, "");

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var handler = new GetMissionNodeValidationsHandler(repo.Object);
        var act = () => handler.Handle(new GetMissionNodeValidationsQuery(mission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*SecretCode*");
    }

    [Fact]
    public async Task Handle_WhenTriviaHasNoQuestions_ThrowsConflict()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var triviaId = mission.AddTriviaNode(stage.Id, [new TriviaQuestion("Q", ["A", "B"], 0)], 1, baseScore: 100);
        mission.FindNodeById(triviaId)!.TriviaQuestions.Clear();

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var handler = new GetMissionNodeValidationsHandler(repo.Object);
        var act = () => handler.Handle(new GetMissionNodeValidationsQuery(mission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*preguntas*");
    }

    [Fact]
    public async Task Handle_WhenTriviaInvalidIndex_ThrowsConflict()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var triviaId = mission.AddTriviaNode(stage.Id, [new TriviaQuestion("Q", ["A", "B"], 0)], 1, baseScore: 100);
        var node = mission.FindNodeById(triviaId)!;
        node.TriviaQuestions.Clear();
        var corrupt = (TriviaQuestion)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TriviaQuestion));
        typeof(TriviaQuestion).GetProperty(nameof(TriviaQuestion.Prompt))!.SetValue(corrupt, "Q");
        typeof(TriviaQuestion).GetProperty(nameof(TriviaQuestion.Options))!.SetValue(corrupt, new List<string> { "A", "B" }.AsReadOnly());
        typeof(TriviaQuestion).GetProperty(nameof(TriviaQuestion.CorrectOptionIndex))!.SetValue(corrupt, 99);
        node.TriviaQuestions.Add(corrupt);

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var handler = new GetMissionNodeValidationsHandler(repo.Object);
        var act = () => handler.Handle(new GetMissionNodeValidationsQuery(mission.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*CorrectOptionIndex*");
    }
}
