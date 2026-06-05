using FluentAssertions;
using MissionManagement.Application.Missions.Queries.GetMissions;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Queries.GetMissions;

public sealed class GetMissionsHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmpty_ReturnsEmptyList()
    {
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MissionManagement.Domain.Aggregates.Mission>());

        var handler = new GetMissionsHandler(repository.Object);
        var result = await handler.Handle(new GetMissionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMissionsExist_MapsToDto()
    {
        var mission = MissionTestData.CreateMissionWithStage(out _);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mission });

        var handler = new GetMissionsHandler(repository.Object);
        var result = await handler.Handle(new GetMissionsQuery(), CancellationToken.None);

        result.Should().ContainSingle(d => d.Id == mission.Id && d.Title == mission.Title);
    }
}
