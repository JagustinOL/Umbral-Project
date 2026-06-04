using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Commands.UpdateTeam;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Commands.UpdateTeam;

public sealed class UpdateTeamHandlerTests
{
    [Fact]
    public async Task Handle_WhenNameDuplicated_ThrowsConflictException()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Líder");
        var repo = new Mock<ITeamRepository>();
        repo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        repo.Setup(x => x.ExistsByNameAsync("Otro", team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var handler = new UpdateTeamHandler(repo.Object);
        var act = () => handler.Handle(new UpdateTeamCommand(team.Id, "Otro", leaderId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesName()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Líder");
        var repo = new Mock<ITeamRepository>();
        repo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        repo.Setup(x => x.ExistsByNameAsync("Nuevo", team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new UpdateTeamHandler(repo.Object);
        await handler.Handle(new UpdateTeamCommand(team.Id, "Nuevo", leaderId), CancellationToken.None);

        team.Name.Should().Be("Nuevo");
        repo.Verify(x => x.SaveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
    }
}
