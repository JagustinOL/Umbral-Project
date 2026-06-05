using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Hints.Commands.AddHint;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Moq;

namespace MissionManagement.Application.Tests.Hints.Commands.AddHint;

public sealed class AddHintHandlerTests
{
    private readonly Mock<IMissionRepository> _repositoryMock = new();

    [Fact]
    public async Task Handle_WhenMissionDoesNotExist_ThrowsNotFoundException()
    {
        var missionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var handler = new AddHintHandler(_repositoryMock.Object);
        var command = new AddHintCommand(
            MissionId: missionId,
            NodeId: Guid.NewGuid(),
            Content: "Pista",
            Attachment: BuildFileMock(contentType: "image/jpeg", length: 1024));

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Id={missionId}*");
    }

    [Fact]
    public async Task Handle_WhenNodeIsStage_ThrowsInvalidOperationException()
    {
        var mission = BuildDraftMissionWithStage(out var stage);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new AddHintHandler(_repositoryMock.Object);
        var command = new AddHintCommand(
            MissionId: mission.Id,
            NodeId: stage.Id,
            Content: "Pista",
            Attachment: null);

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Trivia*");
    }

    [Fact]
    public async Task Handle_WhenInvalidFileFormat_ThrowsConflictException()
    {
        var mission = BuildDraftMissionWithTriviaGame(out var gameNode);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new AddHintHandler(_repositoryMock.Object);
        var command = new AddHintCommand(
            MissionId: mission.Id,
            NodeId: gameNode.Id,
            Content: "Pista",
            Attachment: BuildFileMock(contentType: "application/pdf", length: 1024));

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Formato de archivo no válido*");
    }

    [Fact]
    public async Task Handle_WhenAttachmentIsLargerThan5Mb_ThrowsConflictException()
    {
        var mission = BuildDraftMissionWithTriviaGame(out var gameNode);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new AddHintHandler(_repositoryMock.Object);
        var command = new AddHintCommand(
            MissionId: mission.Id,
            NodeId: gameNode.Id,
            Content: "Pista",
            Attachment: BuildFileMock(contentType: "image/png", length: (5 * 1024 * 1024) + 1));

        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("*supera el tamaño máximo permitido*");
    }

    [Fact]
    public async Task Handle_WhenValidCommand_InvokesDomainAndSaves()
    {
        var mission = BuildDraftMissionWithTriviaGame(out var gameNode);

        _repositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(mission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mission);

        var handler = new AddHintHandler(_repositoryMock.Object);
        var command = new AddHintCommand(
            MissionId: mission.Id,
            NodeId: gameNode.Id,
            Content: "Pista válida",
            Attachment: BuildFileMock(contentType: "image/jpeg", length: 1024));

        var hintId = await handler.Handle(command, CancellationToken.None);

        hintId.Should().NotBe(Guid.Empty);
        gameNode.Hints.Should().ContainSingle(h => h.Id == hintId);
        _repositoryMock.Verify(
            r => r.SaveAsync(mission, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Mission BuildDraftMissionWithStage(out MissionNode stage)
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        return mission;
    }

    private static Mission BuildDraftMissionWithTriviaGame(out MissionNode gameNode)
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc etapa", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);

        var triviaId = mission.AddTriviaNode(
            parentNodeId: stage.Id,
            questions: [new TriviaQuestion("¿Pregunta?", ["A", "B"], correctOptionIndex: 0)],
            executionOrder: 1);

        gameNode = mission.FindNodeById(triviaId)!;
        return mission;
    }

    private static IFormFile BuildFileMock(string contentType, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.SetupGet(x => x.ContentType).Returns(contentType);
        mock.SetupGet(x => x.Length).Returns(length);
        return mock.Object;
    }
}
