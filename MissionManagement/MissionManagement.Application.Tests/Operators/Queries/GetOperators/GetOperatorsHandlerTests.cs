using FluentAssertions;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Operators.Queries.GetOperators;
using Moq;

namespace MissionManagement.Application.Tests.Operators.Queries.GetOperators;

public sealed class GetOperatorsHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();

    [Fact]
    public async Task Handle_WhenCalled_InvokesGetOperatorsOnceAndReturnsResult()
    {
        // Arrange
        IReadOnlyList<OperatorIdentityDto> expected =
        [
            new OperatorIdentityDto(
                OperatorId: Guid.NewGuid(),
                FirstName: "Ada",
                LastName: "Lovelace",
                Email: "ada@umbral.com",
                IsActive: true)
        ];

        _identityServiceMock
            .Setup(s => s.GetOperatorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var handler = new GetOperatorsHandler(_identityServiceMock.Object);
        var query = new GetOperatorsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expected);
        _identityServiceMock.Verify(
            s => s.GetOperatorsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

