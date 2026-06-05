using FluentAssertions;
using Moq;
using SessionManagement.Application.Common;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Common;

public sealed class TeamSessionLockServiceTests
{
    [Fact]
    public async Task LockTeamsForSessionAsync_LocksAndSavesTeams()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        var repo = new Mock<ITeamRepository>();
        repo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { team });

        await TeamSessionLockService.LockTeamsForSessionAsync(session, repo.Object, CancellationToken.None);

        team.IsLocked.Should().BeTrue();
        repo.Verify(r => r.SaveRangeAsync(It.IsAny<IReadOnlyList<Team>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseTeamsFromSessionAsync_ReleasesTeams()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        team.AssignToSession(session.Id);
        team.Lock();
        var repo = new Mock<ITeamRepository>();
        repo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { team });

        await TeamSessionLockService.ReleaseTeamsFromSessionAsync(session, repo.Object, CancellationToken.None);

        team.IsLocked.Should().BeFalse();
        team.CurrentSessionRef.Should().BeNull();
    }

    [Fact]
    public async Task AssignTeamToSessionAsync_WhenTeamExists_AssignsAndSaves()
    {
        var session = LiveSessionTestFactory.BuildPendingSession();
        var team = Team.Create("Squad", Guid.NewGuid(), "Lead");
        var repo = new Mock<ITeamRepository>();
        repo.Setup(r => r.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        await TeamSessionLockService.AssignTeamToSessionAsync(team.Id, session.Id, repo.Object, CancellationToken.None);

        team.CurrentSessionRef.Should().Be(session.Id);
        repo.Verify(r => r.SaveAsync(team, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockTeamsForSessionAsync_WhenNoTeams_DoesNotSave()
    {
        var session = LiveSession.Create(Guid.NewGuid(), Guid.NewGuid(),
            [new SessionManagement.Domain.ValueObjects.AllowedNode(Guid.NewGuid(), "Trivia", 10)], 1m);
        var repo = new Mock<ITeamRepository>();
        repo.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Team>());

        await TeamSessionLockService.LockTeamsForSessionAsync(session, repo.Object, CancellationToken.None);

        repo.Verify(r => r.SaveRangeAsync(It.IsAny<IReadOnlyList<Team>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
