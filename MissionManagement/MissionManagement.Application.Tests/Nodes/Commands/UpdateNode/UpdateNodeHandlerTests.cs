using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Commands.UpdateNode;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Commands.UpdateNode;

public sealed class UpdateNodeHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new UpdateNodeHandler(_repositoryMock.Object);
        var act = () => handler.Handle(new UpdateNodeCommand(missionId, Guid.NewGuid(), "T", "D"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesNodeAndSaves()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new UpdateNodeHandler(_repositoryMock.Object);
        await handler.Handle(new UpdateNodeCommand(mission.Id, stage.Id, "Nuevo título", "Nueva desc"), CancellationToken.None);

        stage.Title.Should().Be("Nuevo título");
        stage.Description.Should().Be("Nueva desc");
        _repositoryMock.Verify(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
    }
}
