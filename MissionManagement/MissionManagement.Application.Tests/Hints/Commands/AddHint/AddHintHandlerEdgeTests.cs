using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Hints.Commands.AddHint;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Hints.Commands.AddHint;

public sealed class AddHintHandlerEdgeTests
{
    [Fact]
    public async Task Handle_WhenNodeNotFound_ThrowsNotFoundException()
    {
        var mission = MissionTestData.CreateMissionWithTriviaGame(out _);
        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var handler = new AddHintHandler(repo.Object);
        var act = () => handler.Handle(new AddHintCommand(mission.Id, Guid.NewGuid(), "H", 10), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*nodo*");
    }
}
