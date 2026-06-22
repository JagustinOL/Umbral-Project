using FluentAssertions;
using MissionManagement.Application.Dtos;
using MissionManagement.Application.Exceptions;

namespace MissionManagement.Application.Tests.Dtos;

public sealed class DtoCoverageTests
{
    [Fact]
    public void AllDtos_CanBeConstructed()
    {
        var missionId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var mission = new MissionDto(missionId, "T", "D", "Draft", "Medium", 1.5m, 60, DateTime.UtcNow, null, []);
        mission.Title.Should().Be("T");

        _ = new MissionNodeDto(nodeId, "N", "D", "Stage", 1, 10, null);
        _ = new HintDto(nodeId, 1, "H", 5);
        _ = new GpsCoordinateDto(1, 2);
        _ = new StageGameDto(nodeId, "Trivia", 1, 10, "Game");
        _ = new TriviaQuestionDto("Q", ["A", "B"], 0);
        _ = new TriviaNodeDto(nodeId, missionId, nodeId, "Trivia", 1, 10, [new TriviaQuestionDto("Q", ["A", "B"], 0)]);
        _ = new TreasureHuntNodeDto(nodeId, missionId, nodeId, "TreasureHunt", 1, 10, "I", "C", new GpsCoordinateDto(1, 2));
        _ = new MissionNodeValidationDto(nodeId, "Trivia", 1, 10, "A");

        var ex = new ExternalDependencyException("dep fail");
        ex.Message.Should().Contain("dep");
    }
}
