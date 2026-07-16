using FluentAssertions;
using Moq;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using MissionManagement.Application.Hints;
using MissionManagement.Domain.Aggregates;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;
using MissionManagement.Domain.ValueObjects;
using Xunit;

namespace MissionManagement.Application.Tests.Hints;

public sealed class MissionHintServiceAndProxyTests
{
    private static Mission BuildDraftWithTriviaAndHint(out MissionNode trivia, out Hint hint)
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);
        var triviaId = mission.AddTriviaNode(
            stage.Id,
            [new TriviaQuestion("¿Q?", ["A", "B"], 0)],
            executionOrder: 1,
            baseScore: 100);
        trivia = mission.FindNodeById(triviaId)!;
        hint = Hint.Create(trivia.Id, 1, "Pista 1", 5);
        mission.AddHintToNode(trivia.Id, hint);
        return mission;
    }

    [Fact]
    public async Task MissionHintService_ReturnsOrderedHints()
    {
        var mission = BuildDraftWithTriviaAndHint(out var trivia, out var hint);
        var second = Hint.Create(trivia.Id, 2, "Pista 2", 3);
        mission.AddHintToNode(trivia.Id, second);

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var service = new MissionHintService(repo.Object);
        var result = await service.GetHintsForNodeAsync(mission.Id, trivia.Id);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(hint.Id);
        result[0].Order.Should().Be(1);
        result[1].Order.Should().Be(2);
    }

    [Fact]
    public async Task MissionHintService_WhenMissionMissing_ThrowsNotFound()
    {
        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var service = new MissionHintService(repo.Object);
        var act = () => service.GetHintsForNodeAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MissionHintService_WhenNodeIsStage_Throws()
    {
        var mission = Mission.Create("Misión", "Descripción", DifficultyLevel.Medium);
        var stage = MissionNode.Create("Etapa 1", "Desc", MissionNodeType.Stage, executionOrder: 1, baseScore: 10);
        mission.AddRootNode(stage);

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var service = new MissionHintService(repo.Object);
        var act = () => service.GetHintsForNodeAsync(mission.Id, stage.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Trivia*");
    }

    [Fact]
    public async Task DraftOnlyHintProxy_WhenAdmin_BypassesDraftCheck()
    {
        var mission = BuildDraftWithTriviaAndHint(out var trivia, out _);
        mission.Activate(); // Active — players blocked, admin allowed

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.IsInRole("admin")).Returns(true);

        var proxy = new DraftOnlyHintProxy(new MissionHintService(repo.Object), repo.Object, user.Object);
        var result = await proxy.GetHintsForNodeAsync(mission.Id, trivia.Id);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task DraftOnlyHintProxy_WhenUnauthenticated_AllowsServiceToService()
    {
        var mission = BuildDraftWithTriviaAndHint(out var trivia, out _);
        mission.Activate();

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        user.Setup(u => u.IsAuthenticated).Returns(false);

        var proxy = new DraftOnlyHintProxy(new MissionHintService(repo.Object), repo.Object, user.Object);
        var result = await proxy.GetHintsForNodeAsync(mission.Id, trivia.Id);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task DraftOnlyHintProxy_WhenPlayerAndActive_ThrowsUnauthorized()
    {
        var mission = BuildDraftWithTriviaAndHint(out var trivia, out _);
        mission.Activate();

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        user.Setup(u => u.IsAuthenticated).Returns(true);

        var proxy = new DraftOnlyHintProxy(new MissionHintService(repo.Object), repo.Object, user.Object);
        var act = () => proxy.GetHintsForNodeAsync(mission.Id, trivia.Id);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task DraftOnlyHintProxy_WhenPlayerAndDraft_ReturnsHints()
    {
        var mission = BuildDraftWithTriviaAndHint(out var trivia, out _);

        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(mission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(mission);

        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        user.Setup(u => u.IsAuthenticated).Returns(true);

        var proxy = new DraftOnlyHintProxy(new MissionHintService(repo.Object), repo.Object, user.Object);
        var result = await proxy.GetHintsForNodeAsync(mission.Id, trivia.Id);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task DraftOnlyHintProxy_WhenPlayerAndMissionMissing_ThrowsNotFound()
    {
        var repo = new Mock<IMissionRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mission?)null);

        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
        user.Setup(u => u.IsAuthenticated).Returns(true);

        var proxy = new DraftOnlyHintProxy(new MissionHintService(repo.Object), repo.Object, user.Object);
        var act = () => proxy.GetHintsForNodeAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
