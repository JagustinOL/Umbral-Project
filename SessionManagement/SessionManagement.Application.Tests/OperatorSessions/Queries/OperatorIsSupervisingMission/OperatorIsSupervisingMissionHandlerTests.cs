using FluentAssertions;
using Moq;
using SessionManagement.Application.OperatorSessions.Queries.OperatorIsSupervisingMission;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Queries.OperatorIsSupervisingMission;

public sealed class OperatorIsSupervisingMissionHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToRepository()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.HasOpenSessionForMissionByOperatorAsync(operatorId, missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new OperatorIsSupervisingMissionHandler(repo.Object);
        var result = await handler.Handle(new OperatorIsSupervisingMissionQuery(operatorId, missionId), CancellationToken.None);

        result.Should().BeTrue();
    }
}
