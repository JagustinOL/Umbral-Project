using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Missions.Commands.ActivateMission;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Missions.Commands.ActivateMission;

public sealed class ActivateMissionHandlerTests
{
    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        var repository = new Mock<IMissionRepository>();
        repository
            .Setup(x => x.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var publisher = new Mock<IDomainEventPublisher>();
        var handler = new ActivateMissionHandler(repository.Object, publisher.Object);
        var command = new ActivateMissionCommand(missionId);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        publisher.Verify(
            x => x.PublishAsync(It.IsAny<IReadOnlyList<MissionManagement.Domain.Common.IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMissionIsValid_ActivatesAndPublishesDomainEvents()
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        mission.AddTriviaNode(
            parentNodeId: stage.Id,
            questions: [new TriviaQuestion("¿Capital?", ["Bogota", "Medellin"], correctOptionIndex: 0)],
            executionOrder: 1,
            baseScore: 100);

        var repository = new Mock<IMissionRepository>();
        repository
            .Setup(x => x.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var publisher = new Mock<IDomainEventPublisher>();
        var handler = new ActivateMissionHandler(repository.Object, publisher.Object);
        var command = new ActivateMissionCommand(mission.Id);

        await handler.Handle(command, CancellationToken.None);

        mission.Status.Should().Be(MissionStatus.Active);
        mission.DomainEvents.Should().BeEmpty();
        repository.Verify(x => x.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(
            x => x.PublishAsync(
                It.Is<IReadOnlyList<MissionManagement.Domain.Common.IDomainEvent>>(events =>
                    events.Count == 1 && events[0].GetType().Name == "MissionActivatedEvent"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
