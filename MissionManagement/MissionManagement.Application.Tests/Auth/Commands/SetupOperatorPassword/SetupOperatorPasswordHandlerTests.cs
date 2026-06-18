using FluentAssertions;
using MissionManagement.Application.Auth.Commands.SetupOperatorPassword;
using MissionManagement.Application.Common.Interfaces;
using Moq;

namespace MissionManagement.Application.Tests.Auth.Commands.SetupOperatorPassword;

public sealed class SetupOperatorPasswordHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IAuthService> _authServiceMock = new();

    [Fact]
    public async Task Handle_WhenValid_activatesOperatorAndReturnsToken()
    {
        var token = new AuthTokenResult(
            AccessToken: "access",
            RefreshToken: "refresh",
            ExpiresIn: 300,
            UserId: Guid.NewGuid(),
            Roles: ["operator"]);

        _authServiceMock
            .Setup(s => s.AuthenticateAsync("op@umbral.com", "SecurePass1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var handler = new SetupOperatorPasswordHandler(_identityServiceMock.Object, _authServiceMock.Object);
        var command = new SetupOperatorPasswordCommand("op@umbral.com", "ABCD-1234", "SecurePass1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().Be(token);
        _identityServiceMock.Verify(
            s => s.SetupOperatorPasswordAsync("op@umbral.com", "ABCD-1234", "SecurePass1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordTooShort_ThrowsArgumentException()
    {
        var handler = new SetupOperatorPasswordHandler(_identityServiceMock.Object, _authServiceMock.Object);
        var command = new SetupOperatorPasswordCommand("op@umbral.com", "ABCD-1234", "short");

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _identityServiceMock.Verify(
            s => s.SetupOperatorPasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
