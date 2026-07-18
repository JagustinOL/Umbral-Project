using FluentAssertions;
using Moq;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.Teams.Queries.GetTeamById;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Teams.Queries.GetTeamById;

public sealed class GetTeamByIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenNotFound_ThrowsNotFoundException()
    {
        var teamRepo = new Mock<ITeamRepository>();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        teamRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Team?)null);

        var handler = new GetTeamByIdHandler(teamRepo.Object, sessionRepo.Object);
        var act = () => handler.Handle(new GetTeamByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsDto()
    {
        var team = Team.Create("Equipo", Guid.NewGuid(), "Líder");
        var teamRepo = new Mock<ITeamRepository>();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        teamRepo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        sessionRepo
            .Setup(x => x.FindOpenSessionIdWithPendingJoinByTeamAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var handler = new GetTeamByIdHandler(teamRepo.Object, sessionRepo.Object);
        var result = await handler.Handle(new GetTeamByIdQuery(team.Id), CancellationToken.None);

        result.TeamId.Should().Be(team.Id);
        result.Name.Should().Be("Equipo");
        result.PendingSessionJoinRef.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTeamHasPendingSessionJoin_ReturnsPendingSessionJoinRef()
    {
        var team = Team.Create("Equipo", Guid.NewGuid(), "Líder");
        var pendingSessionId = Guid.NewGuid();
        var teamRepo = new Mock<ITeamRepository>();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        teamRepo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        sessionRepo
            .Setup(x => x.FindOpenSessionIdWithPendingJoinByTeamAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingSessionId);

        var handler = new GetTeamByIdHandler(teamRepo.Object, sessionRepo.Object);
        var result = await handler.Handle(new GetTeamByIdQuery(team.Id), CancellationToken.None);

        result.PendingSessionJoinRef.Should().Be(pendingSessionId);
    }

    [Fact]
    public async Task Handle_WhenTeamAlreadyAssignedToSession_SkipsPendingLookup()
    {
        var team = Team.Create("Equipo", Guid.NewGuid(), "Líder");
        var assignedSessionId = Guid.NewGuid();
        team.AssignToSession(assignedSessionId);

        var teamRepo = new Mock<ITeamRepository>();
        var sessionRepo = new Mock<ILiveSessionRepository>();
        teamRepo.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var handler = new GetTeamByIdHandler(teamRepo.Object, sessionRepo.Object);
        var result = await handler.Handle(new GetTeamByIdQuery(team.Id), CancellationToken.None);

        result.PendingSessionJoinRef.Should().BeNull();
        sessionRepo.Verify(
            x => x.FindOpenSessionIdWithPendingJoinByTeamAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
