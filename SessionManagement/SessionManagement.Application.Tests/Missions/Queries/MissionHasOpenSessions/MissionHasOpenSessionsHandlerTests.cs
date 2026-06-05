using FluentAssertions;
using Moq;
using SessionManagement.Application.Missions.Queries.MissionHasOpenSessions;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.Missions.Queries.MissionHasOpenSessions;

public sealed class MissionHasOpenSessionsHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToRepository()
    {
        var missionId = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.HasOpenSessionsByMissionAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new MissionHasOpenSessionsHandler(repo.Object);
        var result = await handler.Handle(new MissionHasOpenSessionsQuery(missionId), CancellationToken.None);

        result.Should().BeTrue();
    }
}
