using FluentAssertions;
using Moq;
using SessionManagement.Application.OperatorSessions.Queries.OperatorHasActiveSessions;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Queries.OperatorHasActiveSessions;

public sealed class OperatorHasActiveSessionsHandlerTests
{
    [Fact]
    public async Task Handle_DelegatesToRepository()
    {
        var operatorId = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.HasOpenSessionsByOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new OperatorHasActiveSessionsHandler(repo.Object);
        var result = await handler.Handle(new OperatorHasActiveSessionsQuery(operatorId), CancellationToken.None);

        result.Should().BeFalse();
    }
}
