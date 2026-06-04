using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Queries.GetTriviaNodeById;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Queries.GetTriviaNodeById;

public sealed class GetTriviaNodeByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenWrongNodeType_ThrowsConflictException()
    {
        var mission = MissionTestData.CreateMissionWithStage(out var stage);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetTriviaNodeByIdHandler(repository.Object);
        var act = () => handler.Handle(new GetTriviaNodeByIdQuery(mission.Id, stage.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenTrivia_ReturnsDto()
    {
        var mission = MissionTestData.CreateMissionWithTriviaGame(out var trivia);
        var repository = new Mock<IMissionRepository>();
        repository.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new GetTriviaNodeByIdHandler(repository.Object);
        var result = await handler.Handle(new GetTriviaNodeByIdQuery(mission.Id, trivia.Id), CancellationToken.None);

        result.Id.Should().Be(trivia.Id);
        result.Questions.Should().NotBeEmpty();
    }
}
