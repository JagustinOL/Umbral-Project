using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Queries.GetMissionNodeValidations;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Queries.GetMissionNodeValidations;

public sealed class GetMissionNodeValidationsHandlerTests
{
    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new GetMissionNodeValidationsHandler(repository.Object);
        var act = () => handler.Handle(new GetMissionNodeValidationsQuery(missionId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTriviaAndTreasureExist_ReturnsValidationDtos()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        mission.AddTriviaNode(stage.Id, [new MissionManagement.Domain.ValueObjects.TriviaQuestion("Q", ["A", "B"], 0)], 1);
        mission.AddTreasureHuntNode(stage.Id, "Go", "CODE1", new MissionManagement.Domain.ValueObjects.GpsCoordinate(1, 2), 2);

        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetMissionNodeValidationsHandler(repository.Object);
        var result = await handler.Handle(new GetMissionNodeValidationsQuery(mission.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].ExpectedValue.Should().Be("A");
        result[1].ExpectedValue.Should().Be("CODE1");
    }
}
