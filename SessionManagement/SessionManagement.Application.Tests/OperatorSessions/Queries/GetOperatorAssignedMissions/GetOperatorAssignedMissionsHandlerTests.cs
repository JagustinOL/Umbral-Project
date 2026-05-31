using FluentAssertions;
using Moq;
using SessionManagement.Application.Common.Interfaces;
using SessionManagement.Application.OperatorSessions.Queries.GetOperatorAssignedMissions;
using Xunit;

namespace SessionManagement.Application.Tests.OperatorSessions.Queries.GetOperatorAssignedMissions;

public sealed class GetOperatorAssignedMissionsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallIntegrationServiceAndReturnMappedMissions()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var integrationMock = new Mock<IMissionIntegrationService>();

        integrationMock
            .Setup(x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AssignedMissionData(Guid.NewGuid(), operatorId, "Mission A"),
                new AssignedMissionData(Guid.NewGuid(), operatorId, "Mission B")
            ]);

        var handler = new GetOperatorAssignedMissionsHandler(integrationMock.Object);
        var query = new GetOperatorAssignedMissionsQuery(operatorId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        integrationMock.Verify(
            x => x.GetAssignedMissionsForOperatorAsync(operatorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

