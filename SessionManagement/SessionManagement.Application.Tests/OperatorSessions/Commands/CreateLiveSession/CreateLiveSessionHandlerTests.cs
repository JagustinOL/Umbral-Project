using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.Exceptions;
using SessionManagement.Application.OperatorSessions.Commands.CreateLiveSession;
using SessionManagement.Application.Tests.Support;
using SessionManagement.Domain.Repositories;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Commands.CreateLiveSession;

public sealed class CreateLiveSessionHandlerTests
{
    [Fact]
    public async Task Handle_WhenMissionNotAssigned_ThrowsNotFoundException()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AssignedMissionData>());

        var handler = new CreateLiveSessionHandler(repo.Object, integration.Object);
        var act = () => handler.Handle(new CreateLiveSessionCommand(operatorId, missionId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMissionNotActive_ThrowsConflictException()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, missionId));
        integration.Setup(x => x.GetMissionStatusAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Draft");

        var handler = new CreateLiveSessionHandler(repo.Object, integration.Object);
        var act = () => handler.Handle(new CreateLiveSessionCommand(operatorId, missionId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesSession()
    {
        var operatorId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var repo = new Mock<ILiveSessionRepository>();
        var integration = new Mock<IMissionIntegrationService>();
        integration.Setup(x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.AssignedTo(operatorId, missionId));
        integration.Setup(x => x.GetMissionStatusAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Active");
        integration.Setup(x => x.GetNodeValidationDataAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(LiveSessionTestFactory.DefaultValidationData());
        integration.Setup(x => x.GetMissionDifficultyMultiplierAsync(missionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1.5m);

        var handler = new CreateLiveSessionHandler(repo.Object, integration.Object);
        var result = await handler.Handle(new CreateLiveSessionCommand(operatorId, missionId), CancellationToken.None);

        result.SessionId.Should().NotBe(Guid.Empty);
        result.JoinCode.Should().NotBeNullOrWhiteSpace();
        repo.Verify(x => x.SaveAsync(It.IsAny<SessionManagement.Domain.Aggregates.LiveSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
