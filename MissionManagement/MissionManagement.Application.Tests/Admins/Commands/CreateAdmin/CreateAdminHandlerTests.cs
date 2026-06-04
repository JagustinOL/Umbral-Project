using FluentAssertions;
using MissionManagement.Application.Admins.Commands.CreateAdmin;
using MissionManagement.Application.Common.Interfaces;
using Moq;

namespace MissionManagement.Application.Tests.Admins.Commands.CreateAdmin;

public sealed class CreateAdminHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ThrowsArgumentException()
    {
        var handler = new CreateAdminHandler(_identityServiceMock.Object);
        var command = new CreateAdminCommand("Ada", "Lovelace", "ada@umbral.com", "short");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _identityServiceMock.Verify(
            s => s.CreateAdminAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_InvokesCreateAdmin()
    {
        var expectedId = Guid.NewGuid();
        _identityServiceMock
            .Setup(s => s.CreateAdminAsync("Ada", "Lovelace", "ada@umbral.com", "ValidPass1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var handler = new CreateAdminHandler(_identityServiceMock.Object);
        var command = new CreateAdminCommand("Ada", "Lovelace", "ada@umbral.com", "ValidPass1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(expectedId);
    }
}
