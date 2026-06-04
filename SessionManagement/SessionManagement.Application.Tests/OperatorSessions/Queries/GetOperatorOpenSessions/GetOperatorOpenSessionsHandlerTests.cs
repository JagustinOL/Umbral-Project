using Moq;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorOpenSessions;
using SessionManagement.Domain.Aggregates;
using SessionManagement.Domain.Repositories;
using SessionManagement.Domain.ValueObjects;

namespace SessionManagement.Application.Tests.OperatorSessions.Queries.GetOperatorOpenSessions;

public sealed class GetOperatorOpenSessionsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOpenSessionsForOperator()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var session = LiveSession.Create(
            missionId,
            operatorId,
            [new AllowedNode(Guid.NewGuid(), "Trivia", 10)],
            difficultyMultiplier: 1m);

        var repositoryMock = new Mock<ILiveSessionRepository>();
        repositoryMock
            .Setup(r => r.GetOpenSessionsByOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([session]);

        var handler = new GetOperatorOpenSessionsHandler(repositoryMock.Object);
        var result = await handler.Handle(new GetOperatorOpenSessionsQuery(operatorId), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(session.Id, result[0].SessionId);
        Assert.Equal(missionId, result[0].MissionId);
        Assert.Equal(session.JoinCode, result[0].JoinCode);
        Assert.Equal("Pending", result[0].Status);
    }

    [Fact]
    public async Task Handle_WithNoSessions_ReturnsEmptyList()
    {
        var operatorId = Guid.NewGuid();
        var repositoryMock = new Mock<ILiveSessionRepository>();
        repositoryMock
            .Setup(r => r.GetOpenSessionsByOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetOperatorOpenSessionsHandler(repositoryMock.Object);
        var result = await handler.Handle(new GetOperatorOpenSessionsQuery(operatorId), CancellationToken.None);

        Assert.Empty(result);
    }
}
