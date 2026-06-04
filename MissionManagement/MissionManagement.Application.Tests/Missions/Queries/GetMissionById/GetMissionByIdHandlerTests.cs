using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Queries.GetMissionById;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Queries.GetMissionById;

public sealed class GetMissionByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new GetMissionByIdHandler(repository.Object);
        var act = () => handler.Handle(new GetMissionByIdQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsMissionDto()
    {
        var mission = MissionTestData.CreateMissionWithStage(out _);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetMissionByIdHandler(repository.Object);
        var result = await handler.Handle(new GetMissionByIdQuery(mission.Id), CancellationToken.None);

        result.Id.Should().Be(mission.Id);
        result.Title.Should().Be(mission.Title);
    }
}
