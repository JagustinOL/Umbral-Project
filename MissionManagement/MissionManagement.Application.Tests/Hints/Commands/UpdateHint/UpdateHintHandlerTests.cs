using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Hints.Commands.UpdateHint;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Hints.Commands.UpdateHint;

public sealed class UpdateHintHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new UpdateHintHandler(_repositoryMock.Object);
        var act = () => handler.Handle(new UpdateHintCommand(missionId, Guid.NewGuid(), Guid.NewGuid(), "Nuevo"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesHintAndSaves()
    {
        var mission = MissionTestData.CreateMissionWithHint(out var node, out var hintId);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new UpdateHintHandler(_repositoryMock.Object);
        await handler.Handle(new UpdateHintCommand(mission.Id, node.Id, hintId, "Actualizada"), CancellationToken.None);

        node.Hints.Should().ContainSingle(h => h.Content == "Actualizada");
        _repositoryMock.Verify(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
    }
}
