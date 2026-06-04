using FluentAssertions;
using Moq;
using SessionManagement.Application.LiveSessions.Queries.GetActiveSessions;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.LiveSessions.Queries.GetActiveSessions;

public sealed class GetActiveSessionsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedDtos()
    {
        var session = LiveSessionTestFactory.BuildActiveSession();
        var repo = new Mock<ILiveSessionRepository>();
        repo.Setup(x => x.GetActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { session });

        var handler = new GetActiveSessionsHandler(repo.Object);
        var result = await handler.Handle(new GetActiveSessionsQuery(), CancellationToken.None);

        result.Should().ContainSingle(d => d.SessionId == session.Id);
    }
}
