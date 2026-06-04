using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Hints.Commands.DeleteHint;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Hints.Commands.DeleteHint;

public sealed class DeleteHintHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new DeleteHintHandler(_repositoryMock.Object);
        var act = () => handler.Handle(new DeleteHintCommand(missionId, Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_DeletesHintAndSaves()
    {
        var mission = MissionTestData.CreateMissionWithHint(out var node, out var hintId);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new DeleteHintHandler(_repositoryMock.Object);
        await handler.Handle(new DeleteHintCommand(mission.Id, node.Id, hintId), CancellationToken.None);

        node.Hints.Should().BeEmpty();
        _repositoryMock.Verify(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
    }
}
