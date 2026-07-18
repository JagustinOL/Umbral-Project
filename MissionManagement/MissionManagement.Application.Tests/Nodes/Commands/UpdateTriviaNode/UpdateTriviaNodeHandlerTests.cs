using FluentAssertions;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Nodes.Commands.UpdateTriviaNode;
using MissionManagement.Application.Tests.Support;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Nodes.Commands.UpdateTriviaNode;

public sealed class UpdateTriviaNodeHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionNotFound_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MissionManagement.Domain.Aggregates.Mission?)null);

        var handler = new UpdateTriviaNodeHandler(_repositoryMock.Object);
        var questions = new List<TriviaQuestion> { new("Q", ["A", "B"], 0) };
        var act = () => handler.Handle(
            new UpdateTriviaNodeCommand(missionId, Guid.NewGuid(), questions, BaseScore: 100),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesQuestionsBaseScoreAndSaves()
    {
        var mission = MissionTestData.CreateMissionWithTriviaGame(out var trivia);
        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var newQuestions = new List<TriviaQuestion> { new("Nueva?", ["X", "Y"], 1) };
        var handler = new UpdateTriviaNodeHandler(_repositoryMock.Object);
        await handler.Handle(
            new UpdateTriviaNodeCommand(mission.Id, trivia.Id, newQuestions, BaseScore: 250),
            CancellationToken.None);

        trivia.TriviaQuestions.Should().ContainSingle(q => q.Prompt == "Nueva?");
        trivia.BaseScore.Should().Be(250);
        _repositoryMock.Verify(r => r.SaveAsync(mission, It.IsAny<CancellationToken>()), Times.Once);
    }
}
