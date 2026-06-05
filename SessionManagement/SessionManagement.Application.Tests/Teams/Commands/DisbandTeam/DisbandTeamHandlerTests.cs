using FluentAssertions;
using Moq;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Commands.DisbandTeam;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Commands.DisbandTeam;

public sealed class DisbandTeamHandlerTests
{
    [Fact]
    public async Task Handle_WhenTeamNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<ITeamRepository>();
        var sessions = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var handler = new DisbandTeamHandler(repo.Object, sessions.Object);
        var act = () => handler.Handle(new DisbandTeamCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenValid_DisbandsTeam()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Líder");
        var repo = new Mock<ITeamRepository>();
        var sessions = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var handler = new DisbandTeamHandler(repo.Object, sessions.Object);
        await handler.Handle(new DisbandTeamCommand(team.Id, leaderId), CancellationToken.None);

        team.IsDisbanded.Should().BeTrue();
        repo.Verify(x => x.SaveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenActiveSession_ThrowsConflictException()
    {
        var leaderId = Guid.NewGuid();
        var team = Team.Create("Equipo", leaderId, "Líder");
        var activeSession = LiveSessionTestFactory.BuildActiveSession();
        team.AssignToSession(activeSession.Id);

        var repo = new Mock<ITeamRepository>();
        var sessions = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        sessions.Setup(x => x.GetByIdAsync(activeSession.Id, It.IsAny<CancellationToken>())).ReturnsAsync(activeSession);

        var handler = new DisbandTeamHandler(repo.Object, sessions.Object);
        var act = () => handler.Handle(new DisbandTeamCommand(team.Id, leaderId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*sesión activa*");
    }
}
